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

public class VoteWithdrawSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<VoteWithdrawSyncDataService> _logger;
    private readonly IVoteProvider _voteProvider;
    private readonly IChainAppService _chainAppService;
    private readonly INESTRepository<VoteRecordIndex, string> _voteRecordIndexRepository;
    private const int MaxResultCount = 500;
    
    public VoteWithdrawSyncDataService(ILogger<VoteWithdrawSyncDataService> logger,IGraphQLProvider graphQlProvider,
        IVoteProvider voteProvider, INESTRepository<VoteRecordIndex, string> voteRecordIndexRepository, 
        IChainAppService chainAppService)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _voteProvider = voteProvider;
        _voteRecordIndexRepository = voteRecordIndexRepository;
        _chainAppService = chainAppService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        var blockHeight = -1L;
        List<WithdrawnDto> queryList;
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
            queryList = await _voteProvider.GetSyncVoteWithdrawListAsync(input);
            _logger.LogInformation("VoteWithdrawSyncDataService queryList chainId: {chainId} skipCount: {skipCount} startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
                chainId, skipCount, lastEndHeight, newIndexHeight, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            blockHeight = Math.Max(blockHeight, queryList.Select(t => t.BlockHeight).Max());
            var toUpdate = new List<VoteRecordIndex>();
            foreach (var voteWithdraw in queryList)
            {
                toUpdate.AddRange(await _voteProvider.GetByVoterAndVotingItemIdsAsync(chainId, voteWithdraw.Voter, voteWithdraw.VotingItemIdList));
            }
            foreach (var voteRecordIndex in toUpdate)
            {
                voteRecordIndex.IsWithdraw = true;
            }
            await _voteRecordIndexRepository.BulkAddOrUpdateAsync(toUpdate);
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
        return WorkerBusinessType.VoteWithdrawSync;
    }
}