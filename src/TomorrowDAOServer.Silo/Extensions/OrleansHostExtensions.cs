using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Orleans.Statistics;
using Orleans.Storage;
using Serilog;
using TomorrowDAOServer.Silo.MongoDB;

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
            .Configure<GrainCollectionNameOptions>(options =>
            {
                var collectionName = configSection
                    .GetSection(nameof(GrainCollectionNameOptions.GrainSpecificCollectionName)).GetChildren();
                options.GrainSpecificCollectionName = collectionName.ToDictionary(o => o.Key, o => o.Value);
            })
            .ConfigureServices(services =>
                services.AddSingleton<IGrainStateSerializer, TomorrowDAOJsonGrainStateSerializer>())
            .AddTomorrowDAOMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
            {
                op.CollectionPrefix = "GrainStorage";
                op.DatabaseName = configSection.GetValue<string>("DataBase");
                var grainIdPrefix = configSection
                    .GetSection("GrainSpecificIdPrefix").GetChildren().ToDictionary(o => o.Key.ToLower(), o => o.Value);
                foreach (var kv in grainIdPrefix)
                {
                    Log.Information($"GrainSpecificIdPrefix, key: {kv.Key}, Value: {kv.Value}");
                }

                op.KeyGenerator = id =>
                {
                    var grainType = id.Type.ToString();
                    if (grainIdPrefix.TryGetValue(grainType, out var prefix))
                    {
                        return prefix.StartsWith("GrainReference=000000") ? $"{prefix}+{id.Key}" : prefix;
                    }

                    return id.ToString();
                };
                op.CreateShardKeyForCosmos = configSection.GetValue<bool>("CreateShardKeyForMongoDB", false);
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
            .Configure<GrainCollectionOptions>(opt =>
            {
                var collectionAge = configSection.GetValue<int>("CollectionAge");
                if (collectionAge > 0)
                {
                    opt.CollectionAge = TimeSpan.FromSeconds(collectionAge);
                }
            })
            .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
        });
    }
}