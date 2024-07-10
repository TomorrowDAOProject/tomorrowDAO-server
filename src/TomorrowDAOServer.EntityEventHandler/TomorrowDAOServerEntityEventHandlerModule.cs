using System;
using AElf.Indexing.Elasticsearch.Options;
using TomorrowDAOServer.EntityEventHandler.Core;
using TomorrowDAOServer.EntityEventHandler.Core.Background.Options;
using TomorrowDAOServer.Grains;
using TomorrowDAOServer.MongoDB;
using TomorrowDAOServer.Work;
using TomorrowDAOServer.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Configuration;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.OpenIddict.Tokens;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.CosmosDB;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Volo.Abp.BackgroundJobs.Hangfire;
using TomorrowDAOServer.Common.Enum;
using MongoDB.Driver;
using TomorrowDAOServer.Options;

namespace TomorrowDAOServer.EntityEventHandler;

[DependsOn(typeof(AbpAutofacModule),
    typeof(TomorrowDAOServerMongoDbModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(TomorrowDAOServerEntityEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    //typeof(AbpEventBusRabbitMqModule),
    typeof(TomorrowDAOServerWorkerModule),
    typeof(AbpBackgroundJobsHangfireModule)
    // typeof(AbpBackgroundJobsRabbitMqModule)
)]
public class TomorrowDAOServerEntityEventHandlerModule : AbpModule
{
  public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureTokenCleanupService();
        var configuration = context.Services.GetConfiguration();
        Configure<WorkerOptions>(configuration);
        Configure<WorkerLastHeightOptions>(configuration);
        Configure<WorkerReRunProposalOptions>(configuration.GetSection("WorkerReRunProposalOptions"));
        Configure<TmrwdaoOption>(configuration.GetSection("TmrwdaoOption"));
        Configure<SyncDataOptions>(configuration.GetSection("SyncData"));
        Configure<DaoAliasOptions>(configuration.GetSection("DaoAlias"));
        Configure<IndexerOptions>(configuration.GetSection("IndexerOptions"));
        ConfigureHangfire(context, configuration);
        // Configure<AbpRabbitMqBackgroundJobOptions>(configuration.GetSection("AbpRabbitMqBackgroundJob"));
        context.Services.AddHostedService<TomorrowDAOServerHostedService>();
        context.Services.AddSingleton<IClusterClient>(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];;
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(TomorrowDAOServerGrainsModule).Assembly).WithReferences())
                //.AddSimpleMessageStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });
        ConfigureEsIndexCreation();
        ConfigureGraphQl(context, configuration);
        // ConfigureBackgroundJob(configuration);
    }
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async ()=> await client.Connect());
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }

    //Create the ElasticSearch Index based on Domain Entity
    private void ConfigureEsIndexCreation()
    {
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(TomorrowDAOServerDomainModule)); });
    }
    
    //Disable TokenCleanupService
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
    
    private void ConfigureGraphQl(ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }
    
    private void ConfigureBackgroundJob(IConfiguration configuration)
    {
        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = false;
            var tmrwdaoOption = configuration.GetSection("TmrwdaoOption");
            var isReleaseAuto = tmrwdaoOption.GetSection("IsReleaseAuto").Value;
            if (isReleaseAuto.IsNullOrEmpty())
            {
                return;
            }

            if (!"true".Equals(isReleaseAuto, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            options.IsJobExecutionEnabled = true;
            // options.AddJob(typeof(QueryTransactionStatusJob));
        });
    }
    
    private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var mongoType = configuration["Hangfire:MongoType"];
        var connectionString = configuration["Hangfire:ConnectionString"];
        if (connectionString.IsNullOrEmpty()) return;
    
        if (mongoType.IsNullOrEmpty() ||
            mongoType.Equals(MongoType.MongoDb.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            context.Services.AddHangfire(x =>
            {
                x.UseMongoStorage(connectionString, new MongoStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        MigrationStrategy = new MigrateMongoMigrationStrategy(),
                        BackupStrategy = new CollectionMongoBackupStrategy()
                    },
                    CheckConnection = true,
                    CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
                });
            });
        }
        else if (mongoType.Equals(MongoType.DocumentDb.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            context.Services.AddHangfire(config =>
            {
                var mongoUrlBuilder = new MongoUrlBuilder(connectionString);
                var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());
                var opt = new CosmosStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        BackupStrategy = new NoneMongoBackupStrategy(),
                        MigrationStrategy = new DropMongoMigrationStrategy(),
                    }
                };
                config.UseCosmosStorage(mongoClient, mongoUrlBuilder.DatabaseName, opt);
            });
        }
        
        context.Services.AddHangfireServer(opt =>
        {
            opt.SchedulePollingInterval = TimeSpan.FromMilliseconds(3000);
            opt.HeartbeatInterval = TimeSpan.FromMilliseconds(3000);
            opt.Queues = new[] { "default", "notDefault" };
        });
    }
}