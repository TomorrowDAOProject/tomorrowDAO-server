using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.TonGift.Provider;

namespace TomorrowDAOServer.TonGift;

public class TonGiftTaskCheckService: ScheduleSyncDataService
{
    private readonly ILogger<TonGiftTaskCheckService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly ITonGiftTaskProvider _tonGiftTaskProvider;
    private readonly IOptionsMonitor<TonGiftTaskOptions> _tonGiftTaskOptions;
    private readonly ITonGiftApiProvider _tonGiftApiProvider;
    
    public TonGiftTaskCheckService(ILogger<TonGiftTaskCheckService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, ITonGiftTaskProvider tonGiftTaskProvider, 
        IOptionsMonitor<TonGiftTaskOptions> tonGiftTaskOptions, 
        ITonGiftApiProvider tonGiftApiProvider) : base(logger, graphQlProvider)
    {
        _chainAppService = chainAppService;
        _tonGiftTaskProvider = tonGiftTaskProvider;
        _tonGiftTaskOptions = tonGiftTaskOptions;
        _tonGiftApiProvider = tonGiftApiProvider;
        _logger = logger;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        var taskId = _tonGiftTaskOptions.CurrentValue.TaskId;
        List<TonGiftTaskIndex> queryList;
        do
        {
            queryList = await _tonGiftTaskProvider.GetFailedListAsync(taskId, skipCount);
            _logger.LogInformation("NeedChangeProposalList taskId {taskId} skipCount {skipCount} count: {count}", taskId, skipCount, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            var toValidateVoters = queryList.Select(x => x.Identifier).Distinct().ToList();
            var response = await _tonGiftApiProvider.UpdateTaskAsync(toValidateVoters);
            await _tonGiftTaskProvider.HandleUpdateStatusAsync(response, toValidateVoters, taskId);
            skipCount += queryList.Count;
        } while (!queryList.IsNullOrEmpty());

        return -1L;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.TonGiftTaskCheck;
    }
}