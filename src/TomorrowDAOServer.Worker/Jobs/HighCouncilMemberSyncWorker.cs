using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Work;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Worker.Jobs;

public class HighCouncilMemberSyncWorker : TomorrowDAOServerWorkBase
{
    private readonly ILogger<ScheduleSyncDataContext> _logger;
    private readonly IScriptService _scriptService;
    private IOptionsMonitor<QueryContractOption> _queryContractOption;
    
    protected override WorkerBusinessType BusinessType => WorkerBusinessType.HighCouncilMemberSync;
    
    public HighCouncilMemberSyncWorker(ILogger<ScheduleSyncDataContext> logger, AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory, IScheduleSyncDataContext scheduleSyncDataContext,
        IOptionsMonitor<WorkerOptions> optionsMonitor, IScriptService scriptService) : base(logger, timer, serviceScopeFactory,
        scheduleSyncDataContext, optionsMonitor)
    {
        _logger = logger;
        _scriptService = scriptService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("HC Member async start ...");
        await _scheduleSyncDataContext.DealAsync(BusinessType);
        _logger.LogInformation("HC Member async end ...");
    }
}