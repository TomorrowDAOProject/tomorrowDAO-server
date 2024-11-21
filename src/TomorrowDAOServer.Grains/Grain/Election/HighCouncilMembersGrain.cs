using TomorrowDAOServer.Grains.State.Election;

namespace TomorrowDAOServer.Grains.Grain.Election;

public interface IHighCouncilMembersGrain : IGrainWithStringKey
{
    Task SaveHighCouncilMembersAsync(List<string> addressList);
    Task<List<string>> GetHighCouncilMembersAsync();
}

public class HighCouncilMembersGrain : Grain<HighCouncilMembersState>, IHighCouncilMembersGrain
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