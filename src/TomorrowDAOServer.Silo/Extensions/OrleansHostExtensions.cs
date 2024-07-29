using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Hosting.Kubernetes;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Statistics;
using Serilog;

namespace TomorrowDAOServer.Silo.Extensions;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansSnapshot(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            //Configure OrleansSnapshot
            var configSection = context.Configuration.GetSection("Orleans");
            
            Log.Logger.Warning("==  POD_IP: {0}", Environment.GetEnvironmentVariable("POD_IP"));
            Log.Logger.Warning("==  SiloPort: {0}", configSection.GetValue<int>("SiloPort"));
            Log.Logger.Warning("==  GatewayPort: {0}", configSection.GetValue<int>("GatewayPort"));
            Log.Logger.Warning("==  DatabaseName: {0}", configSection.GetValue<string>("DataBase"));
            Log.Logger.Warning("==  ClusterId: {0}", Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID"));
            Log.Logger.Warning("==  ServiceId: {0}", Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID"));

            var isRunningInKubernetes = configSection.GetValue<bool>("IsRunningInKubernetes");
            var advertisedIp = isRunningInKubernetes ?  Environment.GetEnvironmentVariable("POD_IP") :configSection.GetValue<string>("AdvertisedIP");
            var clusterId = isRunningInKubernetes ? Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID") : configSection.GetValue<string>("ClusterId");
            var serviceId = isRunningInKubernetes ? Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID") : configSection.GetValue<string>("ServiceId");

            siloBuilder
            .ConfigureEndpoints(advertisedIP: IPAddress.Parse(advertisedIp),
                siloPort: configSection.GetValue<int>("SiloPort"),
                gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
            .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
            .UseMongoDBClustering(options =>
            {
                options.DatabaseName = configSection.GetValue<string>("DataBase");
                options.Strategy = MongoDBMembershipStrategy.SingleDocument;
            })
            .AddMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
            {
                op.CollectionPrefix = "GrainStorage";
                op.DatabaseName = configSection.GetValue<string>("DataBase");
                op.ConfigureJsonSerializerSettings = jsonSettings =>
                {
                    // jsonSettings.ContractResolver = new PrivateSetterContractResolver();
                    jsonSettings.NullValueHandling = NullValueHandling.Include;
                    jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                    jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                };
            })
            .UseMongoDBReminders(options =>
            {
                options.DatabaseName = configSection.GetValue<string>("DataBase");
                options.CreateShardKeyForCosmos = false;
            })
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = clusterId;
                options.ServiceId = serviceId;
            })
            // .AddMemoryGrainStorage("PubSubStore")
            .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
            .Configure<GrainCollectionOptions>(opt =>
            {
                var collectionAge = configSection.GetValue<int>("CollectionAge");
                if (collectionAge > 0)
                {
                    opt.CollectionAge = TimeSpan.FromSeconds(collectionAge);
                }
            })
            .Configure<PerformanceTuningOptions>(opt =>
            {
                var minDotNetThreadPoolSize = configSection.GetValue<int>("MinDotNetThreadPoolSize");
                var minIoThreadPoolSize = configSection.GetValue<int>("MinIOThreadPoolSize");
                opt.MinDotNetThreadPoolSize = minDotNetThreadPoolSize > 0 ? minDotNetThreadPoolSize : 200;
                opt.MinIOThreadPoolSize = minIoThreadPoolSize > 0 ? minIoThreadPoolSize : 200;
            })
            .UseDashboard(options =>
            {
                options.Username = configSection.GetValue<string>("DashboardUserName");
                options.Password = configSection.GetValue<string>("DashboardPassword");
                options.Host = "*";
                options.Port = configSection.GetValue<int>("DashboardPort");
                options.HostSelf = true;
                options.CounterUpdateIntervalMs = configSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
            })
            .UseLinuxEnvironmentStatistics()
            .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
        });
    }
}