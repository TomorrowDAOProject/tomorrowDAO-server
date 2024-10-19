using TomorrowDAOServer.Grains.State.ApplicationHandler;
using Orleans;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Grains.Grain.ApplicationHandler;

public interface IBPGrain : IGrainWithStringKey
{
    Task SetBPAsync(List<string> addressList, long round);
    Task<List<string>> GetBPAsync();
    Task<BpInfoDto> GetBPWithRoundAsync();
}

public class BPGrain : Grain<BPState>, IBPGrain
{
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public async Task SetBPAsync(List<string> addressList, long round)
    {
        State.AddressList = addressList;
        State.Round = round;
        await WriteStateAsync();
    }

    public Task<List<string>> GetBPAsync()
    {
        return Task.FromResult(State.AddressList);
    }
    
    public Task<BpInfoDto> GetBPWithRoundAsync()
    {
        return Task.FromResult(new BpInfoDto
        {
            AddressList = State.AddressList,
            Round = State.Round
        });
    }
}