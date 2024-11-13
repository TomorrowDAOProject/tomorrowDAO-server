using Orleans;
using TomorrowDAOServer.Grains.State.Users;

namespace TomorrowDAOServer.Grains.Grain.Users;

public interface IUserViewAdTimeStampGrain : IGrainWithStringKey
{
    Task<bool> UpdateUserViewAdTimeStampAsync(long timeStamp);
}

public class UserViewAdTimeStampGrain : Grain<UserViewAdTimeStampState>, IUserViewAdTimeStampGrain
{
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }
    
    public async Task<bool> UpdateUserViewAdTimeStampAsync(long timeStamp)
    {
        var result = State.TimeStamp < timeStamp;
        if (result)
        {
            State.TimeStamp = timeStamp;
        }

        await WriteStateAsync();
        return result;
    }
}