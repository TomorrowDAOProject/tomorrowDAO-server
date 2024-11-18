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