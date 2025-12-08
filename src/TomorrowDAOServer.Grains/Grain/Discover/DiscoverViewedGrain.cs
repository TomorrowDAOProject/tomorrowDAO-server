using Orleans;
using TomorrowDAOServer.Grains.State.Discussion;

namespace TomorrowDAOServer.Grains.Grain.Discover;

public interface IDiscoverViewedGrain : IGrainWithStringKey
{
    Task<bool> DiscoverViewedAsync();
}

public class DiscoverViewedGrain : Grain<DiscoverViewedState>, IDiscoverViewedGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
    
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
    
    public async Task<bool> DiscoverViewedAsync()
    {
        var viewed = State.Viewed;
        State.Viewed = true;
        await WriteStateAsync();
        return viewed;
    }
}