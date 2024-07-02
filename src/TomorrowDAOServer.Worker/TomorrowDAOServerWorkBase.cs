using TomorrowDAOServer.Work;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Enums;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Worker;

public abstract class TomorrowDAOServerWorkBase : AsyncPeriodicBackgroundWorkerBase
{
    protected abstract WorkerBusinessType BusinessType { get; }

    protected readonly ILogger<ScheduleSyncDataContext> _logger;
    protected readonly IScheduleSyncDataContext _scheduleSyncDataContext;
    private readonly IOptionsMonitor<WorkerLastHeightOptions> _workerLastHeightOptions;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptions;

    protected TomorrowDAOServerWorkBase(ILogger<ScheduleSyncDataContext> logger,
        AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IScheduleSyncDataContext scheduleSyncDataContext,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IOptionsMonitor<WorkerLastHeightOptions> workerLastHeightOptions) :
        base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _scheduleSyncDataContext = scheduleSyncDataContext;
        _workerLastHeightOptions = workerLastHeightOptions;
        _workerOptions = optionsMonitor;
        timer.Period = optionsMonitor.CurrentValue.GetWorkerSettings(BusinessType).TimePeriod;

        //to change timer Period if the WorkerOptions has changed.
        optionsMonitor.OnChange((newOptions, _) =>
        {
            var workerSetting = newOptions.GetWorkerSettings(BusinessType);
            timer.Period = workerSetting.TimePeriod;
            if (workerSetting.OpenSwitch)
            {
                timer.Start();
            }
            else
            {
                timer.Stop();
            }

            _logger.LogInformation(
                "The workerSetting of Worker {BusinessType} has changed to Period = {Period} ms, OpenSwitch = {OpenSwitch}.",
                BusinessType, timer.Period, workerSetting.OpenSwitch);
        });

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
                _logger.LogError(e, "change WorkerLastHeightOptions of Worker {BusinessType} error.",
                    BusinessType.ToString());
            }
        });
    }
    
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var workerSetting = _workerOptions.CurrentValue.GetWorkerSettings(BusinessType);
        if (workerSetting is { OpenSwitch: false })
        {
            return;
        }
        _logger.LogInformation("Background worker [{0}] start ...", BusinessType);
        await _scheduleSyncDataContext.DealAsync(BusinessType);
        _logger.LogInformation("Background worker [{0}] finished ...", BusinessType);
    }
    
}