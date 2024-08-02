using Orleans;
using TomorrowDAOServer.Grains.State.Discussion;

namespace TomorrowDAOServer.Grains.Grain.Discussion;

public interface ICommentCountGrain : IGrainWithStringKey
{
    Task<long> GetNextCount();
    Task<long> GetCurrentCount();
}

public class CommentCountGrain : Grain<CommentState>, ICommentCountGrain
{
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    public async Task<long> GetNextCount()
    {
        State.Count++;
        await WriteStateAsync();
        return State.Count;
    }
    
    public Task<long> GetCurrentCount()
    {
        return Task.FromResult(State.Count);
    }
}