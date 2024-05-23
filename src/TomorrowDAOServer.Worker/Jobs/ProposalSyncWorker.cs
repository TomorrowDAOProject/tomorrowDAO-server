using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Work;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Worker.Jobs;

public class ProposalSyncWorker : TomorrowDAOServerWorkBase
{
    protected override WorkerBusinessType BusinessType => WorkerBusinessType.ProposalSync;
    private readonly IOptionsMonitor<WorkerLastHeightOptions> _workerLastHeightOptions;

    public ProposalSyncWorker(ILogger<ScheduleSyncDataContext> logger,
        AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IScheduleSyncDataContext scheduleSyncDataContext,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IOptionsMonitor<WorkerLastHeightOptions> workerLastHeightOptions) :
        base(logger, timer, serviceScopeFactory, scheduleSyncDataContext, optionsMonitor)
    {
        _workerLastHeightOptions = workerLastHeightOptions;
        _workerLastHeightOptions.OnChange((newOptions, _) =>
        {
            try
            {
                var heightSettings = newOptions.GetHeightSettings(BusinessType);
                _scheduleSyncDataContext.ResetLastEndHeightAsync(BusinessType, heightSettings);
                _logger.LogInformation(
                    "The WorkerLastHeightOptions of Worker {BusinessType} has changed.", BusinessType.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "change WorkerLastHeightOptions of Worker {BusinessType} error.", BusinessType.ToString());
            }
        });
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _scheduleSyncDataContext.DealAsync(BusinessType);
    }
}