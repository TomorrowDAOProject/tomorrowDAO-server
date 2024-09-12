using System;
using System.IO;
using System.Linq;
using AutoResponseWrapper;
using Confluent.Kafka;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using StackExchange.Redis;
using TomorrowDAOServer.Grains;
using TomorrowDAOServer.Middleware;
using TomorrowDAOServer.MongoDB;
using TomorrowDAOServer.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TomorrowDAOServer.Monitor.Http;
using TomorrowDAOServer.Monitor.Orleans.Filters;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Filter;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.BlobStoring.Aliyun;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Kafka;
using Volo.Abp.Kafka;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Threading;
using Volo.Abp.VirtualFileSystem;

namespace TomorrowDAOServer
{
    [DependsOn(
        typeof(TomorrowDAOServerHttpApiModule),
        typeof(AbpAutofacModule),
        typeof(AbpCachingStackExchangeRedisModule),
        typeof(AbpDistributedLockingModule),
        typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
        typeof(TomorrowDAOServerApplicationModule),
        typeof(TomorrowDAOServerMongoDbModule),
        typeof(AbpAspNetCoreSerilogModule),
        typeof(AbpSwashbuckleModule),
        typeof(AbpEventBusKafkaModule),
        // typeof(AbpCachingModule),
        typeof(AbpBlobStoringAliyunModule)
    )]
    public class TomorrowDAOServerHttpApiHostModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var hostingEnvironment = context.Services.GetHostingEnvironment();
            Configure<ChainOptions>(configuration.GetSection("Chains"));
            Configure<TokenInfoOptions>(configuration.GetSection("TokenInfoOptions"));
            Configure<ContractInfoOptions>(configuration.GetSection("ContractInfoOptions"));
            Configure<AssetsInfoOptions>(configuration.GetSection("AssetsInfoOptions"));
            Configure<ExplorerOptions>(configuration.GetSection("Explorer"));
            Configure<AelfApiInfoOptions>(configuration.GetSection("AelfApiInfoOptions"));
            Configure<DaoOptions>(configuration.GetSection("TestDao"));
            Configure<NetworkDaoOptions>(configuration.GetSection("NetworkDao"));
            Configure<TransferTokenOption>(configuration.GetSection("TransferToken"));
            Configure<DaoAliasOptions>(configuration.GetSection("DaoAlias"));
            Configure<RankingOptions>(configuration.GetSection("Ranking"));
            Configure<HubCommonOptions>(configuration.GetSection("HubCommonOptions"));
            
            ConfigureConventionalControllers();
            ConfigureAuthentication(context, configuration);
            ConfigureLocalization();
            ConfigureCache(context, configuration);
            ConfigureVirtualFileSystem(context);
            ConfigureRedis(context, configuration, hostingEnvironment);
            ConfigureCors(context, configuration);
            ConfigureSwaggerServices(context, configuration);
            ConfigureTokenCleanupService();
            ConfigureOrleans(context, configuration);
            ConfigureGraphQl(context, configuration);
            ConfigureDistributedLocking(context, configuration);
            ConfigureKafka(context, configuration);
            
            context.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:Configuration"];
            });
            
            // ConfigFilter(context);
            context.Services.AddAutoResponseWrapper();
        }

        private void ConfigureKafka(ServiceConfigurationContext context, IConfiguration configuration)
        {
            Configure<AbpKafkaOptions>(options =>
            {
                options.Connections.Default.BootstrapServers = configuration.GetValue<string>("Kafka:Connections:Default:BootstrapServers");
                //options.Connections.Default.SaslUsername = "user";
                //options.Connections.Default.SaslPassword = "pwd";
                options.ConfigureProducer = config =>
                {
                    config.MessageTimeoutMs = configuration.GetValue<int>("Kafka:Producer:MessageTimeoutMs");
                    config.MessageSendMaxRetries = configuration.GetValue<int>("Kafka:Producer:MessageSendMaxRetries");
                    config.SocketTimeoutMs = configuration.GetValue<int>("Kafka:Producer:SocketTimeoutMs");
                    config.Acks = Acks.All;
                };
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

        private void ConfigFilter(ServiceConfigurationContext context)
        {
            context.Services.AddScoped<LoggingFilter>();
            context.Services.Configure<MvcOptions>(options => { options.Filters.AddService<LoggingFilter>(); });
        }

        private void ConfigureCache(ServiceConfigurationContext context, IConfiguration configuration)
        {
            var multiplexer = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
            context.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            
            Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "TomorrowDAOServer:"; });
        }

        private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
        {
            var hostingEnvironment = context.Services.GetHostingEnvironment();

            if (hostingEnvironment.IsDevelopment())
            {
                Configure<AbpVirtualFileSystemOptions>(options =>
                {
                    options.FileSets.ReplaceEmbeddedByPhysical<TomorrowDAOServerDomainSharedModule>(
                        Path.Combine(hostingEnvironment.ContentRootPath,
                            $"..{Path.DirectorySeparatorChar}TomorrowDAOServer.Domain.Shared"));
                    options.FileSets.ReplaceEmbeddedByPhysical<TomorrowDAOServerDomainModule>(
                        Path.Combine(hostingEnvironment.ContentRootPath,
                            $"..{Path.DirectorySeparatorChar}TomorrowDAOServer.Domain"));
                    options.FileSets.ReplaceEmbeddedByPhysical<TomorrowDAOServerApplicationContractsModule>(
                        Path.Combine(hostingEnvironment.ContentRootPath,
                            $"..{Path.DirectorySeparatorChar}TomorrowDAOServer.Application.Contracts"));
                    options.FileSets.ReplaceEmbeddedByPhysical<TomorrowDAOServerApplicationModule>(
                        Path.Combine(hostingEnvironment.ContentRootPath,
                            $"..{Path.DirectorySeparatorChar}TomorrowDAOServer.Application"));
                });
            }
        }

        private void ConfigureConventionalControllers()
        {
            Configure<AbpAspNetCoreMvcOptions>(options =>
            {
                options.ConventionalControllers.Create(typeof(TomorrowDAOServerHttpApiModule).Assembly);
            });
        }


        private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddAbpSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "TomorrowDAOServer API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Scheme = "bearer",
                    Description = "Specify the authorization token.",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] { }
                    }
                });
            });
        }

        private void ConfigureLocalization()
        {
            Configure<AbpLocalizationOptions>(options =>
            {
                options.Languages.Add(new LanguageInfo("ar", "ar", "العربية"));
                options.Languages.Add(new LanguageInfo("cs", "cs", "Čeština"));
                options.Languages.Add(new LanguageInfo("en", "en", "English"));
                options.Languages.Add(new LanguageInfo("en-GB", "en-GB", "English (UK)"));
                options.Languages.Add(new LanguageInfo("fi", "fi", "Finnish"));
                options.Languages.Add(new LanguageInfo("fr", "fr", "Français"));
                options.Languages.Add(new LanguageInfo("hi", "hi", "Hindi", "in"));
                options.Languages.Add(new LanguageInfo("it", "it", "Italian", "it"));
                options.Languages.Add(new LanguageInfo("hu", "hu", "Magyar"));
                options.Languages.Add(new LanguageInfo("pt-BR", "pt-BR", "Português"));
                options.Languages.Add(new LanguageInfo("ru", "ru", "Русский"));
                options.Languages.Add(new LanguageInfo("sk", "sk", "Slovak"));
                options.Languages.Add(new LanguageInfo("tr", "tr", "Türkçe"));
                options.Languages.Add(new LanguageInfo("zh-Hans", "zh-Hans", "简体中文"));
                options.Languages.Add(new LanguageInfo("zh-Hant", "zh-Hant", "繁體中文"));
                options.Languages.Add(new LanguageInfo("de-DE", "de-DE", "Deutsch", "de"));
                options.Languages.Add(new LanguageInfo("es", "es", "Español", "es"));
            });
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

        private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .WithOrigins(
                            configuration["App:CorsOrigins"]
                                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                                .Select(o => o.RemovePostFix("/"))
                                .ToArray()
                        )
                        .WithAbpExposedHeaders()
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        private void ConfigureTokenCleanupService()
        {
            Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
        }

        private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddSingleton<IClusterClient>(o =>
            {
                return new ClientBuilder()
                    .ConfigureDefaults()
                    .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = configuration["Orleans:DataBase"];
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = configuration["Orleans:ClusterId"];
                        options.ServiceId = configuration["Orleans:ServiceId"];
                    })
                    .Configure<ClientMessagingOptions>(options =>
                    {
                        var timeout = MessagingOptions.DEFAULT_RESPONSE_TIMEOUT.Seconds;
                        if (int.TryParse(configuration["Orleans:ResponseTimeout"], out int settings))
                        {
                            timeout = settings;
                        }

                        options.ResponseTimeout = TimeSpan.FromSeconds(timeout);
                    })
                    .ConfigureApplicationParts(parts =>
                        parts.AddApplicationPart(typeof(TomorrowDAOServerGrainsModule).Assembly).WithReferences())
                    .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                    .AddMethodFilter(o)
                    .Build();
            });
        }

        private void ConfigureGraphQl(ServiceConfigurationContext context,
            IConfiguration configuration)
        {
            context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
                new NewtonsoftJsonSerializer()));
            context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            var env = context.GetEnvironment();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseCorrelationId();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();

            app.UseAbpRequestLocalization();
            app.UseAuthorization();

            // if (env.IsDevelopment())
            // {
            app.UseSwagger();
            app.UseAbpSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "Support APP API"); });
            // }

            app.UseMiddleware<DeviceInfoMiddleware>();
            app.UseMiddleware<PerformanceMonitorMiddleware>();
            app.UseAuditing();
            app.UseAbpSerilogEnrichers();
            app.UseUnitOfWork();
            app.UseConfiguredEndpoints();

            StartOrleans(context.ServiceProvider);
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            StopOrleans(context.ServiceProvider);
        }

        private static void StartOrleans(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetRequiredService<IClusterClient>();
            AsyncHelper.RunSync(async () => await client.Connect());
        }

        private static void StopOrleans(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetRequiredService<IClusterClient>();
            AsyncHelper.RunSync(client.Close);
        }

        private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = configuration["AuthServer:Authority"];
                    options.RequireHttpsMetadata = Convert.ToBoolean(configuration["AuthServer:RequireHttpsMetadata"]);
                    options.Audience = "TomorrowDAOServer";
                });
        }

        private void ConfigureDistributedLocking(
            ServiceConfigurationContext context,
            IConfiguration configuration)
        {
            context.Services.AddSingleton<IDistributedLockProvider>(sp =>
            {
                var connection = ConnectionMultiplexer
                    .Connect(configuration["Redis:Configuration"]);
                return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
            });
        }
    }
}