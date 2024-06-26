using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.State.ApplicationHandler;
using TomorrowDAOServer.Grains.State.Election;

namespace TomorrowDAOServer.Grains.Grain.Election;

public interface IHighCouncilMembersGrain : IGrainWithStringKey
{
    public Task SaveHighCouncilMembers(List<string> addressList);
    public Task<List<string>> GetHighCouncilMembers();
}

public class HighCouncilMembersGrain : Grain<HighCouncilMembersState>, IHighCouncilMembersGrain
{
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    public async Task SaveHighCouncilMembers(List<string> addressList)
    {
        State.AddressList = addressList;
        State.UpdateTime = DateTime.Now;
        await WriteStateAsync();
    }

    public Task<List<string>> GetHighCouncilMembers()
    {
        return Task.FromResult(State.AddressList);
    }
}