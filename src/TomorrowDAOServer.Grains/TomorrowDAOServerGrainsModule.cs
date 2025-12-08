using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace TomorrowDAOServer.Grains;

[DependsOn(
    typeof(AbpAutoMapperModule),typeof(TomorrowDAOServerApplicationContractsModule))]
public class TomorrowDAOServerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TomorrowDAOServerGrainsModule>(); });
    }
}