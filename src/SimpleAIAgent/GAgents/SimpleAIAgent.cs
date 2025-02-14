using Aevatar.AI.Agent;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using SimpleAIAgent.GAgents.SEvent;
using SimpleAIAgent.GEvents;

namespace SimpleAIAgent.GAgents;

public class SimpleAIAgent : AIGAgentBase<SimpleAIAgentState, SimpleAIAgentSEvent>
{
    private readonly ILogger<SimpleAIAgent> _logger;

    public SimpleAIAgent(ILogger<SimpleAIAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Agent for interacting with AI.");
    }

    [EventHandler]
    public async Task OnChatAIEvent(SimpleAIAgentEvent @event)
    {
        var prompt = @event.Prompt;
        var result = await InvokePromptAsync(prompt) ?? string.Empty;
    }
}