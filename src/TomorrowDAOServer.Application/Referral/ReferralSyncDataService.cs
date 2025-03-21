using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Referral.Dto;
using TomorrowDAOServer.Referral.Indexer;
using TomorrowDAOServer.Referral.Provider;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Referral;

public class ReferralSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ReferralSyncDataService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IChainAppService _chainAppService;
    private readonly IPortkeyProvider _portkeyProvider;
    private readonly IReferralInviteProvider _referralInviteProvider;
    private readonly IReferralLinkProvider _referralLinkProvider;
    private readonly IOptionsMonitor<SyncDataOptions> _syncDataOptions;
    private const int MaxResultCount = 1000;
    
    public ReferralSyncDataService(ILogger<ReferralSyncDataService> logger,
        IObjectMapper objectMapper, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, IPortkeyProvider portkeyProvider,
        IReferralInviteProvider referralInviteProvider, IReferralLinkProvider referralLinkProvider, 
        IOptionsMonitor<SyncDataOptions> syncDataOptions) : base(logger, graphQlProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _chainAppService = chainAppService;
        _portkeyProvider = portkeyProvider;
        _referralInviteProvider = referralInviteProvider;
        _referralLinkProvider = referralLinkProvider;
        _syncDataOptions = syncDataOptions;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndTime, long newIndexHeight)
    {
        List<IndexerReferral> queryList;
        var rerunHeight = _syncDataOptions.CurrentValue.RerunHeight;
        if (rerunHeight == 0)
        {
            lastEndTime = 0;
        }
        var returnTime = -1L;
        var skipCount = 0;
        do
        {
            queryList = await _portkeyProvider.GetSyncReferralListAsync(CommonConstant.CreateAccountMethodName, lastEndTime, 0, skipCount, MaxResultCount);
            Log.Information("SyncReferralData queryList skipCount {skipCount} startTime: {lastEndHeight} endTime: {newIndexHeight} count: {count}",
                skipCount, lastEndTime, 0, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            returnTime = Math.Max(lastEndTime, queryList.Select(x => x.Timestamp).Max());
            skipCount += queryList.Count;
            var inviteList = queryList.Where(x => !string.IsNullOrEmpty(x.ReferralCode)).ToList();
            var ids = queryList.Select(GetReferralInviteId).ToList();
            var exists = await _referralInviteProvider.GetByIdsAsync(ids);
            var toUpdate = queryList
                .Where(x => exists.All(y => GetReferralInviteId(x) != y.Id))
                .ToList();
            if (toUpdate.IsNullOrEmpty())
            {
                continue;
            }
            
            var referralCodes = inviteList.Select(x => x.ReferralCode).Distinct().ToList();
            var currentLinks = await _referralLinkProvider.GetByReferralCodesAsync(chainId, referralCodes);
            var notExistsReferralCodes = referralCodes
                .Where(code => currentLinks.All(link => link.ReferralCode != code))
                .ToList();
            if (!notExistsReferralCodes.IsNullOrEmpty())
            {
                var codeInfos = await _portkeyProvider.GetReferralCodeCaHashAsync(referralCodes);
                var newLinks = _objectMapper.Map<List<ReferralCodeInfo>, List<ReferralLinkCodeIndex>>(codeInfos);
                foreach (var link in newLinks)
                {
                    link.Id = GuidHelper.GenerateId(chainId, link.InviterCaHash);
                    link.ChainId = chainId;
                }

                await _referralLinkProvider.BulkAddOrUpdateAsync(newLinks);
                currentLinks.AddRange(newLinks);
            }
            var currentLinksDict = currentLinks
                .GroupBy(link => link.ReferralCode)
                .ToDictionary(
                    group => group.Key,            
                    group => group.First().InviterCaHash 
                );
            
            var referralInviteList = _objectMapper.Map<List<IndexerReferral>, List<ReferralInviteRelationIndex>>(toUpdate);
            foreach (var index in referralInviteList)
            {
                index.ChainId = chainId;
                index.Id = GetReferralInviteId(index);
                index.InviterCaHash = currentLinksDict.GetValueOrDefault(index.ReferralCode, string.Empty);
            }
            await _referralInviteProvider.BulkAddOrUpdateAsync(referralInviteList);

        } while (!queryList.IsNullOrEmpty());

        return returnTime;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.ReferralSync;
    }

    private string GetReferralInviteId(IndexerReferral x)
    {
        return GuidHelper.GenerateId(x.CaHash, 
            string.IsNullOrEmpty(x.ReferralCode) ? CommonConstant.OrganicTraffic : x.ReferralCode, 
            x.ProjectCode, x.MethodName);
    }
    
    private string GetReferralInviteId(ReferralInviteRelationIndex x)
    {
        return GuidHelper.GenerateId(x.InviteeCaHash, 
            string.IsNullOrEmpty(x.ReferralCode) ? CommonConstant.OrganicTraffic : x.ReferralCode, 
            x.ProjectCode, x.MethodName);
    }
}