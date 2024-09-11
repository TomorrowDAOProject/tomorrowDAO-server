using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Referral.Indexer;
using TomorrowDAOServer.Referral.Provider;
using TomorrowDAOServer.User;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Referral;

public class ReferralSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ReferralSyncDataService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IChainAppService _chainAppService;
    private readonly IPortkeyProvider _portkeyProvider;
    private readonly IReferralInviteProvider _referralInviteProvider;
    private const int MaxResultCount = 1000;
    
    public ReferralSyncDataService(ILogger<ReferralSyncDataService> logger,
        IObjectMapper objectMapper, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, IPortkeyProvider portkeyProvider,
        IReferralInviteProvider referralInviteProvider) : base(logger, graphQlProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _chainAppService = chainAppService;
        _portkeyProvider = portkeyProvider;
        _referralInviteProvider = referralInviteProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndTime, long newIndexHeight)
    {
        List<IndexerReferral> queryList;
        var endTime = DateTime.UtcNow.ToUtcMilliSeconds();
        var skipCount = 0;
        do
        {
            queryList = await _portkeyProvider.GetSyncReferralListAsync(CommonConstant.CreateAccountMethodName, lastEndTime, endTime, skipCount, MaxResultCount);
            _logger.LogInformation("SyncReferralData queryList skipCount {skipCount} startTime: {lastEndHeight} endTime: {newIndexHeight} count: {count}",
                skipCount, lastEndTime, endTime, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            skipCount += queryList.Count;
            var inviteList = queryList.Where(x => !x.ReferralCode.IsNullOrEmpty()).ToList();
            if (inviteList.IsNullOrEmpty())
            {
                continue;
            }
            var ids = inviteList.Select(GetReferralInviteId).ToList();
            var exists = await _referralInviteProvider.GetByIdsAsync(ids);
            var toUpdate = queryList
                .Where(x => exists.All(y => GetReferralInviteId(x) != y.Id))
                .ToList();
            if (toUpdate.IsNullOrEmpty())
            {
                continue;
            }
            
            var referralCodes = inviteList.Select(x => x.ReferralCode).ToList();
            // todo get InviterCaHash from referralCodes
            var referralInviteList = _objectMapper.Map<List<IndexerReferral>, List<ReferralInviteIndex>>(toUpdate);
            foreach (var index in referralInviteList)
            {
                index.ChainId = chainId;
                index.Id = GetReferralInviteId(index);
            }
            await _referralInviteProvider.BulkAddOrUpdateAsync(referralInviteList);

        } while (!queryList.IsNullOrEmpty());

        return lastEndTime;
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
        return GuidHelper.GenerateId(x.CaHash, x.ReferralCode, x.ProjectCode, x.MethodName);
    }
    
    private string GetReferralInviteId(ReferralInviteIndex x)
    {
        return GuidHelper.GenerateId(x.InviteeCaHash, x.ReferralCode, x.ProjectCode, x.MethodName);
    }
}