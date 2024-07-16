using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Work;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Worker.Jobs;

public class DaoTreasuryAssetsAmountRefreshWorker : TomorrowDAOServerWorkBase
{
    private readonly IOptionsMonitor<WorkerOptions> _workerOptions;
    private readonly IDaoTreasuryAssetsAmountRefreshService _refreshService;
    private readonly IOptionsMonitor<TreasuryAmountRefreshOptions> _treasuryAmountRefreshOptions;

    protected override WorkerBusinessType BusinessType => WorkerBusinessType.DaoTreasuryAssetsAmount;

    public DaoTreasuryAssetsAmountRefreshWorker(ILogger<ScheduleSyncDataContext> logger, AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory, IScheduleSyncDataContext scheduleSyncDataContext,
        IOptionsMonitor<WorkerOptions> optionsMonitor, IOptionsMonitor<WorkerLastHeightOptions> workerLastHeightOptions,
        IDaoTreasuryAssetsAmountRefreshService refreshService,
        IOptionsMonitor<TreasuryAmountRefreshOptions> treasuryAmountRefreshOptions)
        : base(logger, timer, serviceScopeFactory,
            scheduleSyncDataContext, optionsMonitor, workerLastHeightOptions)
    {
        _workerOptions = optionsMonitor;
        _refreshService = refreshService;
        _treasuryAmountRefreshOptions = treasuryAmountRefreshOptions;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var workerSetting = _workerOptions.CurrentValue.GetWorkerSettings(BusinessType);
        if (workerSetting is { OpenSwitch: false })
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var symbols = _treasuryAmountRefreshOptions.CurrentValue.Symbols;
        _logger.LogInformation("Background worker [{0}] start ...", BusinessType);
        foreach (var symbol in symbols)
        {
            await _refreshService.RefreshDaoTreasuryAssetsAmount(symbol.Key, symbol.Value);
        }

        stopwatch.Stop();
        _logger.LogInformation("Background worker [{0}] finished ... {1}",
            BusinessType, stopwatch.ElapsedMilliseconds);
    }
}