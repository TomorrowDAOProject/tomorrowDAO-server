using Orleans;
using TomorrowDAOServer.Grains.State.Referral;

namespace TomorrowDAOServer.Grains.Grain.Referral;

public interface IReferralInviteCountGrain : IGrainWithStringKey
{
    Task<long> GetInviteCountAsync();
    Task<long> IncrementInviteCountAsync();
}

public class ReferralInviteCountGrain : Grain<InviteCountState>, IReferralInviteCountGrain
{
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    public Task<long> GetInviteCountAsync()
    {
        return Task.FromResult(State.InviteCount);
    }

    public async Task<long> IncrementInviteCountAsync()
    {
        State.InviteCount++;
        await WriteStateAsync();
        return State.InviteCount;
    }
}