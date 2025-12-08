using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace TomorrowDAOServer.EntityEventHandler.Core
{
    [DependsOn(typeof(AbpAutoMapperModule),
        typeof(TomorrowDAOServerApplicationModule),
        typeof(TomorrowDAOServerApplicationContractsModule))]
    public class TomorrowDAOServerEntityEventHandlerCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<TomorrowDAOServerEntityEventHandlerCoreModule>();
            });
        }
    }
}