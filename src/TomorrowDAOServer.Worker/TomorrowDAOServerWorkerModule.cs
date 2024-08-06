using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.Worker.Jobs;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace TomorrowDAOServer.Worker
{
    [DependsOn(
        typeof(TomorrowDAOServerApplicationContractsModule),
        typeof(AbpBackgroundWorkersModule)
    )]
    public class TomorrowDAOServerWorkerModule : AbpModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var backgroundWorkerManger = context.ServiceProvider.GetRequiredService<IBackgroundWorkerManager>();
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<BPInfoUpdateWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ProposalSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<DAOSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<VoteRecordSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<VoteWithdrawSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ProposalNewUpdateWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<HighCouncilMemberSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TokenPriceUpdateWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ProposalNumUpdateWorker>());
        }
    }
}