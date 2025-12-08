using TomorrowDAOServer.MongoDB;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace TomorrowDAOServer.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(TomorrowDAOServerMongoDbModule),
    typeof(TomorrowDAOServerApplicationContractsModule)
    )]
public class TomorrowDAOServerDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
    }
}
