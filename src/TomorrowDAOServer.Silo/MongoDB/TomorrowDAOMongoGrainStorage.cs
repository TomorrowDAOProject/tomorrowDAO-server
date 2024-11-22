using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;

namespace TomorrowDAOServer.Silo.MongoDB;

public class TomorrowDAOMongoGrainStorage : MongoGrainStorage
{
    private readonly GrainCollectionNameOptions _grainCollectionNameOptions;
    
    public TomorrowDAOMongoGrainStorage(IMongoClientFactory mongoClientFactory, ILogger<MongoGrainStorage> logger,
        MongoDBGrainStorageOptions options, IOptionsSnapshot<GrainCollectionNameOptions> grainCollectionNameOptions)
        : base(mongoClientFactory, logger, options)
    {
        _grainCollectionNameOptions = grainCollectionNameOptions.Value;
    }

    protected override string ReturnGrainName<T>(string stateName, GrainId grainId)
    {
        return _grainCollectionNameOptions.GrainSpecificCollectionName.TryGetValue(typeof(T).FullName,
            out var grainName)
            ? grainName
            : base.ReturnGrainName<T>(stateName, grainId);
    }
}