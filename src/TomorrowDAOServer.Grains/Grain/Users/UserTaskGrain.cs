using Microsoft.Extensions.Logging;
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
    private readonly ILogger<UserTaskGrain> _logger;

    public UserTaskGrain(ILogger<UserTaskGrain> logger)
    {
        _logger = logger;
    }

    public async Task<bool> UpdateUserTaskCompleteTimeAsync(DateTime completeTime, UserTask userTask)
    {
        _logger.LogInformation("UpdateUserTaskCompleteTimeAsync completeTime {0}, userTask {1} completeTime {2}", 
            completeTime, userTask, State.CompleteTime);
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