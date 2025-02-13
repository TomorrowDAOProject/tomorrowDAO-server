using Orleans;
using TomorrowDAOServer.Grains.State.Discussion;

namespace TomorrowDAOServer.Grains.Grain.Discussion;

public interface ICommentCountGrain : IGrainWithStringKey
{
    Task<long> GetNextCount();
}

public class CommentCountGrain : Grain<CommentState>, ICommentCountGrain
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

    public async Task<long> GetNextCount()
    {
        State.Count++;
        await WriteStateAsync();
        return State.Count;
    }
}