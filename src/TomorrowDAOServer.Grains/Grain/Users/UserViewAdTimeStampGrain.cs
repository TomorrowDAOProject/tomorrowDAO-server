using Orleans;
using TomorrowDAOServer.Grains.State.Users;

namespace TomorrowDAOServer.Grains.Grain.Users;

public interface IUserViewAdTimeStampGrain : IGrainWithStringKey
{
    Task<bool> UpdateUserViewAdTimeStampAsync(long timeStamp);
    Task<long> GetDailyViewAdCountAsync();
    Task ClearDailyViewAdCountAsync();
}

public class UserViewAdTimeStampGrain : Grain<UserViewAdTimeStampState>, IUserViewAdTimeStampGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<bool> UpdateUserViewAdTimeStampAsync(long timeStamp)
    {
        var today = DateTime.UtcNow.Date;
        if (State.LastUpdateDate != today)
        {
            State.DailyViewAdCount = 0;
            State.LastUpdateDate = today;
            await WriteStateAsync(); 
        }

        if (State.DailyViewAdCount >= 20)
        {
            return false; 
        }

        var isUpdated = State.TimeStamp < timeStamp;
        if (isUpdated)
        {
            State.TimeStamp = timeStamp;
            State.DailyViewAdCount++; 
        }

        await WriteStateAsync();
        return isUpdated;
    }

    public Task<long> GetDailyViewAdCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        if (State.LastUpdateDate != today)
        {
            return Task.FromResult(0L);
        }
        return Task.FromResult(State.DailyViewAdCount);
    }

    public async Task ClearDailyViewAdCountAsync()
    {
        State.DailyViewAdCount = 0;
        await WriteStateAsync();
    }
}