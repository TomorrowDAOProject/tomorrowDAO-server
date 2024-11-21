using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.LuckyBox.Provider;

namespace TomorrowDAOServer.Luckybox;

public class LuckyboxTaskCompleteService : ScheduleSyncDataService
{
    private readonly ILogger<LuckyboxTaskCompleteService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly ILuckboxTaskProvider _luckboxTaskProvider;
    private readonly ILuckyboxApiProvider _luckyboxApiProvider;
    
    public LuckyboxTaskCompleteService(ILogger<LuckyboxTaskCompleteService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, ILuckboxTaskProvider luckboxTaskProvider, ILuckyboxApiProvider luckyboxApiProvider) 
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _luckboxTaskProvider = luckboxTaskProvider;
        _luckyboxApiProvider = luckyboxApiProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        List<LuckyBoxTaskIndex> queryList;
        do
        {
            queryList = await _luckboxTaskProvider.GetNeedReportAsync(skipCount);
            _logger.LogInformation("NeedReportLuckyboxTaskAsync skipCount {skipCount} count: {count}", skipCount, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }

            var idList = queryList.Select(x => x.TrackId).Distinct().ToList();
            var tasks = idList.Select(async id =>
            {
                var result = await _luckyboxApiProvider.ReportAsync(id);
                return new { id, result };
            });
            var results = await Task.WhenAll(tasks);
            var resultMap = results.ToDictionary(item => item.id, item => item.result);
            foreach (var task in queryList)
            {
                task.UpdateTaskStatus = resultMap.GetValueOrDefault(task.TrackId, false) ? UpdateTaskStatus.Completed : UpdateTaskStatus.Failed;
            }

            await _luckboxTaskProvider.BulkAddOrUpdateAsync(queryList);
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
        return WorkerBusinessType.LuckyboxTaskComplete;
    }
}