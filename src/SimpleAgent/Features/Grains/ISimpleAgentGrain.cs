using Orleans;
using SimpleAgent.GEvents;

namespace SimpleAgent.Features.Grains;

public interface ISimpleAgentGrain : IGrainWithStringKey
{
    Task SaveEvent(SimpleGEvent @event);
}