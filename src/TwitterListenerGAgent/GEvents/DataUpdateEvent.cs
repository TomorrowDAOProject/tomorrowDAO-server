using Aevatar.Core.Abstractions;

namespace TwitterListenerGAgent.GAgents.GEvents;

[GenerateSerializer]
public class DataUpdateEvent : EventBase
{
    [Id(0)] public int DateType { get; set; }
}