using Aevatar.Core.Abstractions;

namespace TwitterListenerGAgent.GAgents.GAgents.SEvent;

[GenerateSerializer]
public class TwitterListenerUpdateKOLSEvent : StateLogEventBase<TwitterListenerUpdateKOLSEvent>
{
    public List<string> KOLs { get; set; }
}