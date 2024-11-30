using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Work;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Worker.Jobs;

public class NetworkDaoSideChainOrgDataSyncWorker : TomorrowDAOServerWorkBase
{
    protected override WorkerBusinessType BusinessType { get; } = WorkerBusinessType.NetworkDaoSideChainOrgSync;
    
    public NetworkDaoSideChainOrgDataSyncWorker(ILogger<ScheduleSyncDataContext> logger, AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory, IScheduleSyncDataContext scheduleSyncDataContext,
        IOptionsMonitor<WorkerOptions> optionsMonitor, IOptionsMonitor<WorkerLastHeightOptions> workerLastHeightOptions)
        : base(logger, timer, serviceScopeFactory, scheduleSyncDataContext, optionsMonitor, workerLastHeightOptions)
    {
    }

}