using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using SimpleAgent.GEvents;

namespace SimpleAgent.Features.Grains;

[StorageProvider(ProviderName = "PubSubStore")]
public class SimpleAgentGrain : Grain, ISimpleAgentGrain
{
    private ILogger<SimpleAgentGrain> _logger;

    public SimpleAgentGrain(ILogger<SimpleAgentGrain> logger)
    {
        _logger = logger;
    }

    public Task SaveEvent(SimpleGEvent @event)
    {
        _logger.LogInformation("Event: {0}", @event.Hello);
        return Task.CompletedTask;
    }
}