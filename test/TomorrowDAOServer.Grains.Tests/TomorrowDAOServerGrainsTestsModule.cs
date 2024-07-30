using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Options;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.EntityEventHandler;
using TomorrowDAOServer.EntityEventHandler.Core;
using TomorrowDAOServer.Grains;
using TomorrowDAOServer.ThirdPart.Exchange;
using TomorrowDAOServer.User;
using Volo.Abp;
using Volo.Abp.Auditing;
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
    typeof(TomorrowDAOServerOrleansTestBaseModule),
    typeof(AElfIndexingElasticsearchModule),
    typeof(TomorrowDAOServerApplicationModule),
    typeof(TomorrowDAOServerApplicationContractsModule)
)]
public class TomorrowDAOServerGrainsTestsModule : AbpModule
{

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAuditingOptions>(options =>
        {
            options.IsEnabled = false;
        });
    }

}
