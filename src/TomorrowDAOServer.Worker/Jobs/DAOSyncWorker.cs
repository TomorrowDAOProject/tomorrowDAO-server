using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Work;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Worker.Jobs;

public class DAOSyncWorker : TomorrowDAOServerWorkBase
{
    protected override WorkerBusinessType BusinessType => WorkerBusinessType.DAOSync;

    public DAOSyncWorker(ILogger<ScheduleSyncDataContext> logger,
        AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IScheduleSyncDataContext scheduleSyncDataContext,
        IOptionsMonitor<WorkerOptions> optionsMonitor) :
        base(logger, timer, serviceScopeFactory, scheduleSyncDataContext, optionsMonitor)
    {
        
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _scheduleSyncDataContext.DealAsync(BusinessType);
    }
}