using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Vote.Index;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Vote;

public class VoteRecordSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<VoteRecordSyncDataService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IVoteProvider _voteProvider;
    private readonly IChainAppService _chainAppService;
    private readonly INESTRepository<VoteRecordIndex, string> _voteRecordIndexRepository;
    private readonly IDaoAliasProvider _daoAliasProvider;
    private const int MaxResultCount = 500;
    
    public VoteRecordSyncDataService(ILogger<VoteRecordSyncDataService> logger,
        IObjectMapper objectMapper, IGraphQLProvider graphQlProvider,
        IVoteProvider voteProvider, INESTRepository<VoteRecordIndex, string> voteRecordIndexRepository, 
        IChainAppService chainAppService)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _voteProvider = voteProvider;
        _voteRecordIndexRepository = voteRecordIndexRepository;
        _chainAppService = chainAppService;
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
                await _voteRecordIndexRepository.BulkAddOrUpdateAsync(_objectMapper.Map<List<IndexerVoteRecord>, List<VoteRecordIndex>>(toUpdate));
            }
            skipCount += queryList.Count;
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
        return WorkerBusinessType.VoteRecordSync;
    }
}