using TomorrowDAOServer.Grains.State.ApplicationHandler;
using Orleans;

namespace TomorrowDAOServer.Grains.Grain.ApplicationHandler;

public class ContractServiceGraphQLGrain : Grain<GraphQlState>, IContractServiceGraphQLGrain
{
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        ReadStateAsync();
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task SetStateAsync(long height)
    {
        State.EndHeight = height;
        await WriteStateAsync();
    }

    public Task<long> GetStateAsync()
    {
        return Task.FromResult(State.EndHeight);
    }
}