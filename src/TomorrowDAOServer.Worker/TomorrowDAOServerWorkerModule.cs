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
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ReferralSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<UserBalanceSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ReferralTopInviterGenerateWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ProposalRedisUpdateWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TopProposalGenerateWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TelegramAppsSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<FindminiAppsSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TonGiftTaskGenerateWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TonGiftTaskCompleteWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<AppUrlUploadWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<LuckyboxTaskCompleteWorker>());
        }
    }
}