using Aevatar.Core.Abstractions;
using Orleans;
using SimpleAgent.GAgents.SEvent;

namespace SimpleAgent.GAgents;

[GenerateSerializer]
public class SimpleAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; } = new Guid();
    [Id(1)] public string UserName { get; set; }
    [Id(2)] public string Text { get; set; }

    public void Apply(SimpleSEvent simpleSEvent)
    {
        Text = simpleSEvent.Text;
    }
}