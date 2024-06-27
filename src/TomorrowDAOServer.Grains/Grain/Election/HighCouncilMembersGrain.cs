using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.State.ApplicationHandler;
using TomorrowDAOServer.Grains.State.Election;

namespace TomorrowDAOServer.Grains.Grain.Election;

public interface IHighCouncilMembersGrain : IGrainWithStringKey
{
    Task SaveHighCouncilMembersAsync(List<string> addressList);
    Task<List<string>> GetHighCouncilMembersAsync();
}

public class HighCouncilMembersGrain : Grain<HighCouncilMembersState>, IHighCouncilMembersGrain
{
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    public async Task SaveHighCouncilMembersAsync(List<string> addressList)
    {
        State.AddressList = addressList;
        State.UpdateTime = DateTime.Now;
        await WriteStateAsync();
    }

    public Task<List<string>> GetHighCouncilMembersAsync()
    {
        return Task.FromResult(State.AddressList);
    }
}