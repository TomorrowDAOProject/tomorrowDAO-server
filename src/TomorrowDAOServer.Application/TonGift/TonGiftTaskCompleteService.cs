using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.TonGift.Provider;
using TomorrowDAOServer.Vote;
using TomorrowDAOServer.Vote.Provider;

namespace TomorrowDAOServer.TonGift;

public class TonGiftTaskCompleteService : ScheduleSyncDataService
{
    private readonly ILogger<TonGiftTaskCompleteService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly IVoteProvider _voteProvider;
    private readonly IOptionsMonitor<TonGiftTaskOptions> _tonGiftTaskOptions;
    private readonly ITonGiftTaskProvider _tonGiftTaskProvider;
    private readonly IPortkeyProvider _portkeyProvider;
    private readonly ITonGiftApiProvider _tonGiftApiProvider;
    
    public TonGiftTaskCompleteService(ILogger<TonGiftTaskCompleteService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, IVoteProvider voteProvider, IOptionsMonitor<TonGiftTaskOptions> tonGiftTaskOptions, 
        ITonGiftTaskProvider tonGiftTaskProvider, IPortkeyProvider portkeyProvider, ITonGiftApiProvider tonGiftApiProvider) 
        : base(logger, graphQlProvider)
    {
        _chainAppService = chainAppService;
        _voteProvider = voteProvider;
        _tonGiftTaskOptions = tonGiftTaskOptions;
        _tonGiftTaskProvider = tonGiftTaskProvider;
        _portkeyProvider = portkeyProvider;
        _tonGiftApiProvider = tonGiftApiProvider;
        _logger = logger;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var start = _tonGiftTaskOptions.CurrentValue.IsStart;
        if (!start)
        {
            _logger.LogInformation("TonGiftTaskNotStart");
            return lastEndHeight;
        }
        
        var proposalId = _tonGiftTaskOptions.CurrentValue.ProposalId;
        var taskId = _tonGiftTaskOptions.CurrentValue.TaskId;
        var skipCount = 0;
        var blockHeight = lastEndHeight;
        List<VoteRecordIndex> queryList;
        do
        {
            queryList = await _voteProvider.GetByProposalIdAndHeightAsync(proposalId, blockHeight, skipCount, CommonConstant.MaxResultCount);
            _logger.LogInformation("TonGiftTaskComplete queryList skipCount {skipCount} startBlockHeight: {lastEndHeight} count: {count}",
                skipCount, lastEndHeight, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }

            var voters = queryList.Select(x => x.Voter).Distinct().ToList();
            blockHeight = Math.Max(blockHeight, queryList.Select(t => t.BlockHeight).Max());
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
        return WorkerBusinessType.TonGiftTaskComplete;
    }
}