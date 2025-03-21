using AElf.ExceptionHandler.ABP;
using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.Common.Cache;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.Digi;
using TomorrowDAOServer.Election;
using TomorrowDAOServer.Grains;
using TomorrowDAOServer.Luckybox;
using TomorrowDAOServer.Monitor;
using TomorrowDAOServer.Monitor.Http;
using TomorrowDAOServer.Monitor.Logging;
using TomorrowDAOServer.NetworkDao.Sync;
using TomorrowDAOServer.Proposal;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Referral;
using TomorrowDAOServer.ResourceToken;
using TomorrowDAOServer.Spider;
using TomorrowDAOServer.ThirdPart.Exchange;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.TonGift;
using TomorrowDAOServer.User;
using TomorrowDAOServer.Vote;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace TomorrowDAOServer;

[DependsOn(
    typeof(TomorrowDAOServerDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(TomorrowDAOServerApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(TomorrowDAOServerGrainsModule),
    typeof(AbpSettingManagementApplicationModule)
)]
public class TomorrowDAOServerApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
        Configure<QueryContractOption>(configuration.GetSection("QueryContractOption"));
        Configure<ApiOption>(configuration.GetSection("Api"));
        Configure<ExchangeOptions>(configuration.GetSection("Exchange"));
        Configure<CoinGeckoOptions>(configuration.GetSection("CoinGecko"));
        Configure<PerformanceMonitorMiddlewareOptions>(configuration.GetSection("PerformanceMonitorMiddleware"));
        Configure<MonitorForLoggingOptions>(configuration.GetSection("MonitorForLoggingOptions"));
        Configure<AwsS3Option>(configuration.GetSection("AwsS3"));
        Configure<TelegramOptions>(configuration.GetSection("Telegram"));
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TomorrowDAOServerApplicationModule>(); });
        context.Services.AddTransient<IScheduleSyncDataService, DigiTaskCompleteService>();
        context.Services.AddTransient<IScheduleSyncDataService, ResourceTokenParseService>();
        context.Services.AddTransient<IScheduleSyncDataService, ResourceTokenSyncDataService>();
        context.Services.AddTransient<IScheduleSyncDataService, LuckyboxTaskCompleteService>();
        context.Services.AddTransient<IScheduleSyncDataService, AppUrlUploadService>();
        context.Services.AddTransient<IScheduleSyncDataService, TonGiftTaskCompleteService>();
        context.Services.AddTransient<IScheduleSyncDataService, TonGiftTaskGenerateService>();
        context.Services.AddTransient<IScheduleSyncDataService, FindminiAppsSyncDataService>();
        context.Services.AddTransient<IScheduleSyncDataService, TelegramAppsSyncDataService>();
        context.Services.AddTransient<IScheduleSyncDataService, TopProposalGenerateService>();
        context.Services.AddTransient<IScheduleSyncDataService, ProposalRedisUpdateService>();
        context.Services.AddTransient<IScheduleSyncDataService, ReferralTopInviterGenerateService>();
        context.Services.AddTransient<IScheduleSyncDataService, UserBalanceSyncDataService>();
        context.Services.AddTransient<IScheduleSyncDataService, ReferralSyncDataService>();
        context.Services.AddTransient<IScheduleSyncDataService, ProposalSyncDataService>();
        context.Services.AddTransient<IScheduleSyncDataService, ProposalNewUpdateService>();
        context.Services.AddTransient<IScheduleSyncDataService, DAOSyncDataService>();
        context.Services.AddTransient<IScheduleSyncDataService, BPInfoUpdateService>();
        context.Services.AddTransient<IScheduleSyncDataService, HighCouncilMemberSyncService>();
        context.Services.AddTransient<IScheduleSyncDataService, VoteRecordSyncDataService>();
        context.Services.AddTransient<IScheduleSyncDataService, VoteWithdrawSyncDataService>();
        context.Services.AddTransient<IScheduleSyncDataService, TokenPriceUpdateService>();
        context.Services.AddTransient<IScheduleSyncDataService, ProposalNumUpdateService>();
        context.Services.AddTransient<IScheduleSyncDataService, NetworkDaoMainChainProposalSyncService>();
        context.Services.AddTransient<IScheduleSyncDataService, NetworkDaoSideChainProposalSyncService>();
        context.Services.AddTransient<IScheduleSyncDataService, NetworkDaoMainChainOrgSyncService>();
        context.Services.AddTransient<IScheduleSyncDataService, NetworkDaoSideChainOrgSyncService>();
        context.Services.AddTransient<IExchangeProvider, OkxProvider>();
        context.Services.AddTransient<IExchangeProvider, BinanceProvider>();
        context.Services.AddTransient<IExchangeProvider, CoinGeckoProvider>();
        context.Services.AddTransient<IExchangeProvider, AwakenProvider>();
        context.Services.AddTransient<IExchangeProvider, AetherLinkProvider>();
        context.Services.AddSingleton<IMonitor, MonitorForLogging>();
        context.Services.AddHttpClient();
        context.Services.AddMemoryCache();
        context.Services.AddSingleton(typeof(ILocalMemoryCache<>), typeof(LocalMemoryCache<>));
    }
}
