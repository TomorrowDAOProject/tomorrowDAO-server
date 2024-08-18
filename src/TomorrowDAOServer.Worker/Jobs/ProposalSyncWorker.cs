using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal;
using TomorrowDAOServer.Work;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Worker.Jobs;

public class ProposalSyncWorker : TomorrowDAOServerWorkBase
{
    protected override WorkerBusinessType BusinessType => WorkerBusinessType.ProposalSync;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private readonly IProposalAssistService _proposalAssistService;


    public ProposalSyncWorker(ILogger<ScheduleSyncDataContext> logger,
        AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IScheduleSyncDataContext scheduleSyncDataContext,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IOptionsMonitor<WorkerLastHeightOptions> workerLastHeightOptions,
        IOptionsMonitor<RankingOptions> rankingOptions,
        IProposalAssistService proposalAssistService) :
        base(logger, timer, serviceScopeFactory, scheduleSyncDataContext, optionsMonitor, workerLastHeightOptions)
    {
        _proposalAssistService = proposalAssistService;
        _rankingOptions = rankingOptions;
        _rankingOptions.OnChange((newOptions, _) =>
        {
            try
            {
                var pattern = newOptions.DescriptionPattern;
                if (string.IsNullOrEmpty(pattern))
                {
                    _logger.LogInformation(
                        "RankingOptionsChangeNoNeedToChangeRegex pattern {pattern}", pattern);
                }
                else
                {
                    _proposalAssistService.ChangeRegex(pattern);
                    _logger.LogInformation("RankingOptionsChangeChangeRegex pattern {pattern}", pattern);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "RankingOptionsChangeException pattern {pattern}", newOptions.DescriptionPattern);
            }
        });
    }
}