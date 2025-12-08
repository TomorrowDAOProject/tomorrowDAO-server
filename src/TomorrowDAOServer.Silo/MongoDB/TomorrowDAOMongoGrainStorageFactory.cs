using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Storage;

namespace TomorrowDAOServer.Silo.MongoDB;

public static class TomorrowDAOMongoGrainStorageFactory
{
    public static IGrainStorage Create(IServiceProvider services, string name)
    {
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<MongoDBGrainStorageOptions>>();
        return ActivatorUtilities.CreateInstance<TomorrowDAOMongoGrainStorage>(services, optionsMonitor.Get(name));
    }
}