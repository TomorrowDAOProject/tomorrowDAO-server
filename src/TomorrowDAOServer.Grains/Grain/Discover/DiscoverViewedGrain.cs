using Orleans;
using TomorrowDAOServer.Grains.State.Discussion;

namespace TomorrowDAOServer.Grains.Grain.Discover;

public interface IDiscoverViewedGrain : IGrainWithStringKey
{
    Task<bool> DiscoverViewedAsync();
}

public class DiscoverViewedGrain : Grain<DiscoverViewedState>, IDiscoverViewedGrain
{
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }
    
    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }
    
    public async Task<bool> DiscoverViewedAsync()
    {
        var viewed = State.Viewed;
        State.Viewed = true;
        await WriteStateAsync();
        return viewed;
    }
}