using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    private readonly IUserAppService _userAppService;
    private readonly IReferralInviteProvider _referralInviteProvider;
    private readonly IReferralLinkProvider _referralLinkProvider;
    
    public ReferralSyncDataService(ILogger<ReferralSyncDataService> logger,
        IObjectMapper objectMapper, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, IPortkeyProvider portkeyProvider,
        IUserAppService userAppService, IReferralInviteProvider referralInviteProvider,
        IReferralLinkProvider referralLinkProvider) : base(logger, graphQlProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _chainAppService = chainAppService;
        _portkeyProvider = portkeyProvider;
        _userAppService = userAppService;
        _referralInviteProvider = referralInviteProvider;
        _referralLinkProvider = referralLinkProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var blockHeight = -1L;
        List<IndexerReferral> queryList;
        do
        {
            queryList = await _portkeyProvider.GetSyncReferralListAsync("CreateCAHolder", 0);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            var validList = queryList.Where(x => !x.ReferralCode.IsNullOrEmpty()).ToList();
            if (validList == null || validList.IsNullOrEmpty())
            {
                continue;
            }
            var ids = validList.Select(GetReferralInviteId).ToList();
            var existsInvites = await _referralInviteProvider.GetByIdsAsync(ids);
            var toUpdate = validList
                .Where(x => existsInvites.All(y => GetReferralInviteId(x) != y.Id))
                .ToList();
            if (!toUpdate.IsNullOrEmpty())
            {
                var caHashes = validList.Select(x => x.CaHash).ToList();
                var referralCodes = validList.Select(x => x.ReferralCode).ToList();
                var userList = await _userAppService.GetUserByCaHashListAsync(caHashes);
                var userAddressDic = userList.ToDictionary(x => x.CaHash, 
                    x => x.AddressInfos?.FirstOrDefault(x => x.ChainId == chainId)?.Address ?? string.Empty);
                var linkDic = (await _referralLinkProvider.GetByReferralCodesAsync(chainId, referralCodes))
                    .ToDictionary(x => x.ReferralCode, x => new { x.ReferralLink, x.Inviter });
                var inviteList = _objectMapper.Map<List<IndexerReferral>, List<ReferralInviteIndex>>(toUpdate);
                foreach (var index in inviteList)
                {
                    index.Invitee = userAddressDic.GetValueOrDefault(index.InviteeCaHash, string.Empty);
                    index.ChainId = chainId;
                    var linkInfo = linkDic.GetValueOrDefault(index.ReferralCode, new { ReferralLink = string.Empty, Inviter = string.Empty });
                    index.Inviter = linkInfo.Inviter;
                    index.ReferralLink = linkInfo.ReferralLink;
                    index.Id = GetReferralInviteId(index);
                }
                await _referralInviteProvider.BulkAddOrUpdateAsync(inviteList);
            }
           
        } while (!queryList.IsNullOrEmpty());

        return blockHeight;
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