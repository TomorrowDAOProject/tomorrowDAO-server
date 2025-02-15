using Aevatar.Core;
using Aevatar.Core.Abstractions;
using AevatarTemplate.GAgents.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TwitterListenerGAgent.GAgents.GAgents.SEvent;
using TwitterListenerGAgent.GAgents.GEvents;

namespace TwitterListenerGAgent.GAgents.GAgents;

public class TwitterListenerGAgent : GAgentBase<TwitterListenerGAgentState, TwitterListenerUpdateKOLSEvent, EventBase,
    TwitterListenerOptions>
{
    private readonly ILogger<TwitterListenerGAgent> _logger;

    public TwitterListenerGAgent(ILogger<TwitterListenerGAgent> logger) : base(logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Monitor KOL tweets");
    }


    public override async Task InitializeAsync(TwitterListenerOptions initializationEvent)
    {
        _logger.LogInformation("Initialize TwitterListenerGAgent，{0}",
            JsonConvert.SerializeObject(initializationEvent));
        RaiseEvent(new TwitterListenerUpdateKOLSEvent
        {
            Ctime = DateTime.UtcNow,
            KOLs = initializationEvent.KOLs
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(StartListeningEvent @event)
    {
        
    }
    
}