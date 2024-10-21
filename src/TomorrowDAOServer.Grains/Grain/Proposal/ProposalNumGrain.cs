using Orleans;
using TomorrowDAOServer.Grains.State.Proposal;

namespace TomorrowDAOServer.Grains.Grain.Proposal;

public interface IProposalNumGrain : IGrainWithStringKey
{
    Task SetProposalNumAsync(long parliamentCount, long associationCount, long referendumCount);
    Task<long> GetProposalNumAsync();
}

public class ProposalNumGrain : Grain<ProposalNumState>, IProposalNumGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task SetProposalNumAsync(long parliamentCount, long associationCount, long referendumCount)
    {
        State.ParliamentCount = parliamentCount;
        State.AssociationCount = associationCount;
        State.ReferendumCount = referendumCount;
        await WriteStateAsync();
    }

    public Task<long> GetProposalNumAsync()
    {
        return Task.FromResult(State.ParliamentCount + State.AssociationCount + State.ReferendumCount);
    }
}