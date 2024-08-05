using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Orleans;
using TomorrowDAOServer.Grains.State.Election;

namespace TomorrowDAOServer.Grains.Grain.Election;

public interface IHighCouncilMembersGrain : IGrainWithStringKey
{
    Task SaveHighCouncilMembersAsync(List<string> addressList);
    Task<List<string>> GetHighCouncilMembersAsync();
}

public class HighCouncilMembersGrain : Grain<HighCouncilMembersState>, IHighCouncilMembersGrain
{
    private readonly ILogger<HighCouncilMembersGrain> _logger;

    public HighCouncilMembersGrain(ILogger<HighCouncilMembersGrain> logger)
    {
        _logger = logger;
    }

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
        Stopwatch sw = Stopwatch.StartNew();
        var result = Task.FromResult(State.AddressList);
        sw.Stop();
        _logger.LogInformation("GetHighCouncilMembers service duration:{0}", sw.ElapsedMilliseconds);
        return result;
    }
}