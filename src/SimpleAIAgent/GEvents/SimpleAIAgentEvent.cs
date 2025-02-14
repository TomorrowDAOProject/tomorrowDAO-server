using Aevatar.Core.Abstractions;

namespace SimpleAIAgent.GEvents;

[GenerateSerializer]
public class SimpleAIAgentEvent : EventBase
{
    [Id(0)] public string EventId { get; set; }
    [Id(1)] public string Prompt { get; set; }
}