using AElf.Indexing.Elasticsearch.Options;
using Elasticsearch.Net;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.EntityEventHandler.Core;
using TomorrowDAOServer.EntityEventHandler.Core.MQ;
using TomorrowDAOServer.Worker;
using TomorrowDAOServer.Worker.Jobs;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.EntityEventHandler.Tests;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule),
    typeof(TomorrowDAOServerDomainModule),
    typeof(TomorrowDAOServerDomainSharedModule),
    typeof(TomorrowDAOServerApplicationContractsModule),
    typeof(TomorrowDAOServerApplicationModule)
    // typeof(TomorrowDAOServerWorkerModule)
    // typeof(TomorrowDAOServerEntityEventHandlerModule)
)]
public class TomorrowDAOServerEntityEventHandlerTestsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAuditingOptions>(options => { options.IsEnabled = false; });

        // Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TomorrowDAOServerApplicationModule>(); });
        // Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TomorrowDAOServerEntityEventHandlerModule>(); });
        // Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TomorrowDAOServerEntityEventHandlerCoreModule>(); });
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(TomorrowDAOServerEntityEventHandlerTestsModule)); });

        context.Services.AddSingleton<VoteAndLikeMessageHandler>();
        context.Services.AddMemoryCache();
        // Do not modify this!!!
        context.Services.Configure<EsEndpointOption>(options =>
        {
            options.Uris = new List<string> { "http://127.0.0.1:9200" };
        });

        context.Services.Configure<IndexSettingOptions>(options =>
        {
            options.NumberOfReplicas = 1;
            options.NumberOfShards = 1;
            options.Refresh = Refresh.True;
            options.IndexPrefix = "tomorrowdaoservertest";
        });
        
        ConfigureGraphQl(context);

        base.ConfigureServices(context);
    }
    
    private void ConfigureGraphQl(ServiceConfigurationContext context)
    {
        context.Services.Configure<GraphQLOptions>(o =>
        {
            o.Configuration = "http://127.0.0.1:8083/AElfIndexer_DApp/PortKeyIndexerCASchema/graphql";
        });
        
        context.Services.AddSingleton(new GraphQLHttpClient(
            "http://127.0.0.1:8083/AElfIndexer_DApp/PortKeyIndexerCASchema/graphql",
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var backgroundWorkerManger = context.ServiceProvider.GetRequiredService<IBackgroundWorkerManager>();
    }
}