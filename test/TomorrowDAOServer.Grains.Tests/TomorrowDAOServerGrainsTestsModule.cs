using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule),
    typeof(TomorrowDAOServerDomainModule),
    typeof(TomorrowDAOServerDomainTestModule),
    typeof(TomorrowDAOServerOrleansTestBaseModule)
    // typeof(TomorrowDAOServerApplicationModule),
    // typeof(TomorrowDAOServerApplicationContractsModule)
)]
public class TomorrowDAOServerGrainsTestsModule : AbpModule
{

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }

}
