using System;
using AElf.Indexing.Elasticsearch.Options;
using Confluent.Kafka;
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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Volo.Abp.BackgroundJobs.Hangfire;
using TomorrowDAOServer.Common.Enum;
using MongoDB.Driver;
using StackExchange.Redis;
using TomorrowDAOServer.Options;
using Volo.Abp.Caching;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Kafka;
using Volo.Abp.Kafka;

namespace TomorrowDAOServer.EntityEventHandler;

[DependsOn(typeof(AbpAutofacModule),
    typeof(TomorrowDAOServerMongoDbModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(TomorrowDAOServerEntityEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    //typeof(AbpEventBusRabbitMqModule),
    typeof(AbpEventBusKafkaModule),
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
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        Configure<WorkerOptions>(configuration);
        Configure<WorkerLastHeightOptions>(configuration);
        Configure<WorkerReRunProposalOptions>(configuration.GetSection("WorkerReRunProposalOptions"));
        Configure<TmrwdaoOption>(configuration.GetSection("TmrwdaoOption"));
        Configure<SyncDataOptions>(configuration.GetSection("SyncData"));
        Configure<DaoAliasOptions>(configuration.GetSection("DaoAlias"));
        Configure<IndexerOptions>(configuration.GetSection("IndexerOptions"));
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<NetworkDaoOptions>(configuration.GetSection("TokenPrice"));
        Configure<ExplorerOptions>(configuration.GetSection("Explorer"));
        Configure<RankingOptions>(configuration.GetSection("Ranking"));
        Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
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
        ConfigureCache(context, configuration);
        ConfigureRedis(context, configuration, hostingEnvironment);
        context.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:Configuration"];
        });
        ConfigureKafka(context, configuration);
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
    
    private void ConfigureCache(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var multiplexer = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
        context.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "TomorrowDAOServer:"; });
    }
    
    private void ConfigureRedis(
        ServiceConfigurationContext context,
        IConfiguration configuration,
        IWebHostEnvironment hostingEnvironment)
    {
        var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("TomorrowDAOServer");
        if (!hostingEnvironment.IsDevelopment())
        {
            var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
            dataProtectionBuilder.PersistKeysToStackExchangeRedis(redis, "TomorrowDAOServer-Protection-Keys");
        }
    }
    
    private void ConfigureKafka(ServiceConfigurationContext context, IConfiguration configuration)
    {
        Configure<AbpKafkaOptions>(options =>
        {
            options.Connections.Default.BootstrapServers = configuration.GetValue<string>("Kafka:Connections:Default:BootstrapServers");
            //options.Connections.Default.SaslUsername = "user";
            //options.Connections.Default.SaslPassword = "pwd";
            options.ConfigureConsumer = config =>
            {
                config.SocketTimeoutMs = configuration.GetValue<int>("Kafka:Consumer:SocketTimeoutMs");
                config.Acks = Acks.All;
                config.GroupId = configuration.GetValue<string>("Kafka:EventBus:GroupId");
                config.EnableAutoCommit = true;
                config.AutoCommitIntervalMs = configuration.GetValue<int>("Kafka:Consumer:AutoCommitIntervalMs");
            };
            options.ConfigureTopic = topic =>
            {
                topic.Name = configuration.GetValue<string>("Kafka:EventBus:TopicName");
                topic.ReplicationFactor = -1;
                topic.NumPartitions = 1;
            };
        });
    }
}