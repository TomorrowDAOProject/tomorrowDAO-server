using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Enums;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer;

public interface IScheduleSyncDataContext
{
    Task DealAsync(WorkerBusinessType businessType);
    Task ResetLastEndHeightAsync(WorkerBusinessType businessType, IDictionary<string, long> blockHeights);
}

public class ScheduleSyncDataContext : ITransientDependency, IScheduleSyncDataContext
{
    private readonly Dictionary<WorkerBusinessType, IScheduleSyncDataService> _syncDataServiceMap;
    private readonly ILogger<ScheduleSyncDataContext> _logger;

    public ScheduleSyncDataContext(IEnumerable<IScheduleSyncDataService> scheduleSyncDataServices,
        ILogger<ScheduleSyncDataContext> logger)
    {
        _syncDataServiceMap = scheduleSyncDataServices.ToDictionary(a => a.GetBusinessType(), a => a);
        _logger = logger;
    }

    public async Task DealAsync(WorkerBusinessType businessType)
    {
        var stopwatch = Stopwatch.StartNew();
        await _syncDataServiceMap.GetOrDefault(businessType).DealDataAsync();
        stopwatch.Stop();
        _logger.LogInformation("It took {Elapsed} ms to execute synchronized data for businessType: {businessType}",
            stopwatch.ElapsedMilliseconds, businessType);
    }

    public async Task ResetLastEndHeightAsync(WorkerBusinessType businessType, IDictionary<string, long> blockHeights)
    {
        var chainIds = await _syncDataServiceMap.GetOrDefault(businessType).GetChainIdsAsync();
        foreach (var chainId in chainIds.Where(chainId => blockHeights != null && blockHeights.ContainsKey(chainId)))
        {
            await _syncDataServiceMap.GetOrDefault(businessType)
                .ResetLastEndHeightAsync(chainId, businessType, blockHeights[chainId]);
        }
    }
}