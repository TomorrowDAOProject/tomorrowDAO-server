using TomorrowDAOServer.Grains.State.ApplicationHandler;
using Orleans;

namespace TomorrowDAOServer.Grains.Grain.ApplicationHandler;

public class ContractServiceGraphQLGrain : Grain<GraphQlState>, IContractServiceGraphQLGrain
{
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync(); 
        await base.OnActivateAsync();
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