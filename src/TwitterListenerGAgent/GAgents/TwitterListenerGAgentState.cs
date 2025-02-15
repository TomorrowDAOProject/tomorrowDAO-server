using Aevatar.Core.Abstractions;
using TwitterListenerGAgent.GAgents.GAgents.SEvent;

namespace TwitterListenerGAgent.GAgents.GAgents;

[GenerateSerializer]
public class TwitterListenerGAgentState : StateBase
{
    public List<string> KOLs { get; set; }

    public void Apply(TwitterListenerUpdateKOLSEvent updateKOLsEvent)
    {
        KOLs = updateKOLsEvent.KOLs;
    }
}