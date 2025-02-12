using System.ComponentModel;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Twitter.GEvents;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using SimpleAgent.Features.Grains;
using SimpleAgent.GAgents;
using SimpleAgent.GAgents.SEvent;
using SimpleAgent.GEvents;

namespace AIHelloWord.GAgents;

[Description("")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(SimpleAgent))]
public class SimpleAgent : GAgentBase<SimpleAgentState, SimpleSEvent>
{
    private readonly ILogger<SimpleAgent> _logger;

    public SimpleAgent(ILogger<SimpleAgent> logger)
    {
        _logger = logger;
    }

    [EventHandler]
    public async Task HandleEventAsync(SimpleGEvent @event)
    {
        var grain = GrainFactory.GetGrain<ISimpleAgentGrain>(Guid.NewGuid().ToString());
        await grain.SaveEvent(@event);

        var id = Guid.NewGuid();
        RaiseEvent(new SimpleSEvent
        {
            Id = id,
            Ctime = DateTime.UtcNow,
            Text = @event.Hello
        });
        await ConfirmEvents();

        await PublishAsync(new CreateTweetGEvent
        {
            Text = @event.Hello
        });
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Example of agent development.");
    }
}