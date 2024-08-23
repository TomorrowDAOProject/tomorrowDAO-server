using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.Vote.Index;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Vote;

public partial class VoteRecordSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<VoteRecordSyncDataService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IVoteProvider _voteProvider;
    private readonly IChainAppService _chainAppService;
    private readonly INESTRepository<VoteRecordIndex, string> _voteRecordIndexRepository;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private readonly IProposalProvider _proposalProvider;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private const int MaxResultCount = 500;
    
    public VoteRecordSyncDataService(ILogger<VoteRecordSyncDataService> logger,
        IObjectMapper objectMapper, IGraphQLProvider graphQlProvider,
        IVoteProvider voteProvider, INESTRepository<VoteRecordIndex, string> voteRecordIndexRepository, 
        IChainAppService chainAppService, IOptionsMonitor<RankingOptions> rankingOptions, IProposalProvider proposalProvider, 
        ITelegramAppsProvider telegramAppsProvider)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _voteProvider = voteProvider;
        _voteRecordIndexRepository = voteRecordIndexRepository;
        _chainAppService = chainAppService;
        _rankingOptions = rankingOptions;
        _proposalProvider = proposalProvider;
        _telegramAppsProvider = telegramAppsProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        var blockHeight = -1L;
        List<IndexerVoteRecord> queryList;
        do
        {
            var input = new GetChainBlockHeightInput
            {
                ChainId = chainId,
                SkipCount = skipCount,
                MaxResultCount = MaxResultCount,
                StartBlockHeight = lastEndHeight,
                EndBlockHeight = newIndexHeight
            };
            queryList = await _voteProvider.GetSyncVoteRecordListAsync(input);
            _logger.LogInformation("VoteRecordSyncDataService queryList chainId: {chainId} skipCount: {skipCount} startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
                chainId, skipCount, lastEndHeight, newIndexHeight, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            blockHeight = Math.Max(blockHeight, queryList.Select(t => t.BlockHeight).Max());
            var existsVoteRecords = await _voteProvider.GetByVotingItemIdsAsync(chainId, queryList.Select(x => x.VotingItemId).ToList());
            var toUpdate = queryList.Where(x => existsVoteRecords.All(y => x.Id != y.Id)).ToList();
            if (!toUpdate.IsNullOrEmpty())
            {
                var voteRecordList = _objectMapper.Map<List<IndexerVoteRecord>, List<VoteRecordIndex>>(toUpdate);
                await UpdateValidRankingVote(chainId, voteRecordList);
                await _voteRecordIndexRepository.BulkAddOrUpdateAsync(voteRecordList);
            }
            skipCount += queryList.Count;
        } while (!queryList.IsNullOrEmpty());

        return blockHeight;
    }

    private async Task UpdateValidRankingVote(string chainId, List<VoteRecordIndex> list)
    {
        try
        {
            var rankingDaoIds = _rankingOptions.CurrentValue.DaoIds;
            var proposalIds = list.Where(x => rankingDaoIds.Contains(x.DAOId) && x.Option == VoteOption.Approved && x.Amount == 1)
                .Select(x => x.VotingItemId).Distinct().ToList();
            var rankingProposalIds = (await _proposalProvider.GetProposalByIdsAsync(chainId, proposalIds))
                .Where(x => x.ProposalCategory == ProposalCategory.Ranking)
                .Select(x => x.ProposalId).ToList();
            var validMemoList = list.Where(x => rankingProposalIds.Contains(x.VotingItemId) && !string.IsNullOrEmpty(x.Memo) 
                    && Regex.IsMatch(x.Memo, CommonConstant.MemoPattern))
                .Select(x => new { Record = x, Alias = Regex.Match(x.Memo, CommonConstant.MemoPattern).Groups[1].Value })
                .ToList();
            var aliasList = validMemoList.Select(x => x.Alias).ToList();
            var telegramApps = await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput{Aliases = aliasList});
            var validAliasDic = telegramApps.Item2.ToDictionary(x => x.Alias, x => x.Title);
            foreach (var item in validMemoList.Where(x => validAliasDic.ContainsKey(x.Alias)))
            {
                item.Record.ValidRankingVote = true;
                item.Record.Alias = item.Alias;
                item.Record.Title = validAliasDic.GetValueOrDefault(item.Alias);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UpdateValidRankingVoteException");
        }
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.VoteRecordSync;
    }
}