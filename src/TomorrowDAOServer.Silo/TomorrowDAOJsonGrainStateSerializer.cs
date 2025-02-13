using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Serialization;
using Orleans.Providers.MongoDB.StorageProviders;

namespace TomorrowDAOServer.Silo;

public class TomorrowDAOJsonGrainStateSerializer : IGrainStateSerializer
{
    private readonly JsonSerializerSettings jsonSettings;

    public TomorrowDAOJsonGrainStateSerializer(IOptions<JsonGrainStateSerializerOptions> options, IServiceProvider serviceProvider)
    {
        jsonSettings = OrleansJsonSerializerSettings.GetDefaultSerializerSettings(serviceProvider);
        options.Value.ConfigureJsonSerializerSettings(jsonSettings);
    }

    public T Deserialize<T>(BsonValue value)
    {
        using var jsonReader = new JTokenReader(value.ToJToken());
        var localSerializer = JsonSerializer.CreateDefault(jsonSettings);
        return localSerializer.Deserialize<T>(jsonReader);
    }

    public BsonValue Serialize<T>(T state)
    {
        var localSerializer = JsonSerializer.CreateDefault(jsonSettings);
        return JObject.FromObject(state, localSerializer).ToBson();
    }  
}