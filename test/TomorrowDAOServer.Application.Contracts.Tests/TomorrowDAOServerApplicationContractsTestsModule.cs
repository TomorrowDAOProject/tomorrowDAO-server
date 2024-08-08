using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.Monitor;
using TomorrowDAOServer.Monitor.Http;
using TomorrowDAOServer.Monitor.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Application.Contracts.Tests;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule),
    typeof(TomorrowDAOServerDomainModule),
    typeof(TomorrowDAOServerDomainTestModule),
    typeof(TomorrowDAOServerApplicationContractsModule)
)]
public class TomorrowDaoServerApplicationContractsTestsModule : AbpModule
{

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAuditingOptions>(options =>
        {
            options.IsEnabled = false;
        });
        
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TomorrowDaoServerApplicationContractsTestsModule>(); });
        
        context.Services.AddSingleton<PerformanceMonitorMiddleware>();
        context.Services.AddSingleton<IMonitor, MonitorForLogging>();

        context.Services.AddMemoryCache();
        
        base.ConfigureServices(context);
    }

}
