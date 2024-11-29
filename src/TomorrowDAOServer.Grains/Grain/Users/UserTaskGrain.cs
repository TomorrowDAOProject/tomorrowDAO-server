using Orleans;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.State.Users;

namespace TomorrowDAOServer.Grains.Grain.Users;

public interface IUserTaskGrain : IGrainWithStringKey
{
    Task<bool> UpdateUserTaskCompleteTimeAsync(DateTime completeTime, UserTask userTask);
}

public class UserTaskGrain : Grain<UserTaskState>, IUserTaskGrain
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
    
    public async Task<bool> UpdateUserTaskCompleteTimeAsync(DateTime completeTime, UserTask userTask)
    {
        switch (userTask)
        {
            case UserTask.Daily:
                var lastCompleteTime = State.CompleteTime;
                if (completeTime <= lastCompleteTime || completeTime.Date == lastCompleteTime.Date)
                {
                    return false;
                }

                State.CompleteTime = completeTime;
                await WriteStateAsync(); 
                return true;
            case UserTask.Explore:
                if (State.CompleteTime != default)
                {
                    return false;
                }

                State.CompleteTime = completeTime;
                await WriteStateAsync();
                return true;
        }

        return false;
    }
}