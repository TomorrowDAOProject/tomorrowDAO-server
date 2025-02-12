using Aevatar.Core.Abstractions;
using Orleans;

namespace SimpleAgent.GAgents.SEvent;

[GenerateSerializer]
public class SimpleSEvent : StateLogEventBase<SimpleSEvent>
{
    [Id(0)] public string Text { get; set; }
}