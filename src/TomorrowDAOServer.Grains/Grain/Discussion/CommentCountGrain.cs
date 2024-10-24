using Orleans;
using TomorrowDAOServer.Grains.State.Discussion;

namespace TomorrowDAOServer.Grains.Grain.Discussion;

public interface ICommentCountGrain : IGrainWithStringKey
{
    Task<long> GetNextCount();
}

public class CommentCountGrain : Grain<CommentState>, ICommentCountGrain
{
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public async Task<long> GetNextCount()
    {
        State.Count++;
        await WriteStateAsync();
        return State.Count;
    }
}