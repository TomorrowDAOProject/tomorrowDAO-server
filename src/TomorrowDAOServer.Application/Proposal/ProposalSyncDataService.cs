using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Proposal;

public class ProposalSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IProposalProvider _proposalProvider;
    private readonly IVoteProvider _voteProvider;
    private readonly IChainAppService _chainAppService;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private readonly IOptionsMonitor<SyncDataOptions> _syncDataOptionsMonitor;
    private readonly IProposalAssistService _proposalAssistService;
    private readonly IRankingAppService _rankingAppService;
    private const int MaxResultCount = 1000;

    public ProposalSyncDataService(ILogger<ProposalSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        IProposalProvider proposalProvider,
        IChainAppService chainAppService,
        IDistributedCache<List<string>> distributedCache,
        IOptionsMonitor<SyncDataOptions> syncDataOptionsMonitor,
        IObjectMapper objectMapper, IVoteProvider voteProvider,
        IProposalAssistService proposalAssistService,
        IRankingAppService rankingAppService)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _proposalProvider = proposalProvider;
        _chainAppService = chainAppService;
        _distributedCache = distributedCache;
        _syncDataOptionsMonitor = syncDataOptionsMonitor;
        _objectMapper = objectMapper;
        _voteProvider = voteProvider;
        _proposalAssistService = proposalAssistService;
        _rankingAppService = rankingAppService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        var blockHeight = -1L;
        // lastEndHeight = 0;
        List<IndexerProposal> queryList;
        do
        {
            queryList = await _proposalProvider.GetSyncProposalDataAsync(skipCount, chainId, lastEndHeight, 0,
                MaxResultCount);
            _logger.LogInformation(
                "SyncProposalData queryList skipCount {skipCount} startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
                skipCount, lastEndHeight, newIndexHeight, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }

            blockHeight = Math.Max(blockHeight, queryList.Select(t => t.BlockHeight).Max());
            var (convertProposalList, rankingProposalList) = await _proposalAssistService.ConvertProposalList(chainId, queryList);
            await _proposalProvider.BulkAddOrUpdateAsync(convertProposalList);
            await _rankingAppService.GenerateRankingApp(chainId, rankingProposalList);
            skipCount += queryList.Count;
        } while (!queryList.IsNullOrEmpty());

        return blockHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        //add multiple chains
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.ProposalSync;
    }
}