using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal;
using TomorrowDAOServer.Work;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Worker.Jobs;

public class ProposalNewUpdateWorker : TomorrowDAOServerWorkBase
{
    protected override WorkerBusinessType BusinessType => WorkerBusinessType.ProposalNewUpdate;
    private readonly IOptionsMonitor<WorkerReRunProposalOptions> _workerReRunProposalOptions;
    private readonly IProposalAssistService _proposalAssistService;

    public ProposalNewUpdateWorker(ILogger<ScheduleSyncDataContext> logger,
        AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IScheduleSyncDataContext scheduleSyncDataContext,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IOptionsMonitor<WorkerReRunProposalOptions> workerReRunProposalOptions,
        IProposalAssistService proposalAssistService) :
        base(logger, timer, serviceScopeFactory, scheduleSyncDataContext, optionsMonitor)
    {
        _proposalAssistService = proposalAssistService;
        _workerReRunProposalOptions = workerReRunProposalOptions;
        _workerReRunProposalOptions.OnChange((newOptions, _) =>
        {
            try
            {
                var chainId = newOptions.ChainId;
                var reRunProposalIds = newOptions.ReRunProposalIds;
                if (string.IsNullOrEmpty(chainId) || reRunProposalIds.IsNullOrEmpty())
                {
                    _logger.LogInformation("WorkerProposalIdsOptionsChange noNeedToReRun chainId {chainId} count {count}", chainId, reRunProposalIds.Count);
                }
                else
                {
                    _proposalAssistService.ReRunProposalList(chainId, reRunProposalIds);
                    _logger.LogInformation("WorkerProposalIdsOptionsChange ReRunEnd chainId {chainId} count {count}", chainId, reRunProposalIds.Count);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "WorkerProposalIdsOptionsWrongChange Exception chainId {chainId} count {count}", newOptions.ChainId, newOptions.ReRunProposalIds.Count);
            }
        });
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _scheduleSyncDataContext.DealAsync(BusinessType);
    }
}