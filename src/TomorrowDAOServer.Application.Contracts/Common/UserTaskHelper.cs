using System.Collections.Generic;
using System.Linq;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.Common;

public class TaskPointsHelper
{
    private static readonly Dictionary<UserTask, List<UserTaskDetail>> TaskDetailMapping = new()
    {
        { UserTask.None, new List<UserTaskDetail> { UserTaskDetail.None }},
        { UserTask.Daily, new List<UserTaskDetail> { UserTaskDetail.DailyVote, UserTaskDetail.DailyFirstInvite, UserTaskDetail.DailyViewAsset } },
        { UserTask.Explore, new List<UserTaskDetail>
            {
                UserTaskDetail.ExploreJoinTgChannel, UserTaskDetail.ExploreFollowX, UserTaskDetail.ExploreJoinDiscord,
                UserTaskDetail.ExploreCumulateFiveInvite, UserTaskDetail.ExploreCumulateTenInvite, UserTaskDetail.ExploreCumulateTwentyInvite
            } 
        }
    };

    public static readonly List<UserTaskDetail> FrontEndTaskDetails = new()
    {
        UserTaskDetail.DailyViewAsset, 
        UserTaskDetail.ExploreJoinTgChannel, UserTaskDetail.ExploreFollowX,
        UserTaskDetail.ExploreFollowX, UserTaskDetail.ExploreJoinDiscord,
    };

    public static List<UserTaskDetail> GetUserTaskDetailFromUserTask(UserTask userTask)
    {
        return TaskDetailMapping.GetValueOrDefault(userTask, new List<UserTaskDetail>());
    }
    
    public static UserTask? GetUserTaskFromUserTaskDetail(UserTaskDetail userTaskDetail)
    {
        foreach (var pair in TaskDetailMapping.Where(pair => pair.Value.Contains(userTaskDetail)))
        {
            return pair.Key;
        }
        return null;
    }
    
    public static PointsType? GetPointsTypeFromUserTaskDetail(UserTaskDetail userTaskDetail)
    {
        return userTaskDetail switch
        {
            UserTaskDetail.DailyVote => PointsType.Vote,
            UserTaskDetail.DailyFirstInvite => PointsType.DailyFirstInvite,
            UserTaskDetail.DailyViewAsset => PointsType.DailyViewAsset,
            UserTaskDetail.ExploreJoinTgChannel => PointsType.ExploreJoinTgChannel,
            UserTaskDetail.ExploreFollowX => PointsType.ExploreFollowX,
            UserTaskDetail.ExploreJoinDiscord => PointsType.ExploreJoinDiscord,
            UserTaskDetail.ExploreCumulateFiveInvite => PointsType.ExploreCumulateFiveInvite,
            UserTaskDetail.ExploreCumulateTenInvite => PointsType.ExploreCumulateTenInvite,
            UserTaskDetail.ExploreCumulateTwentyInvite => PointsType.ExploreCumulateTwentyInvite,
            _ => null
        };
    }
    
    public static List<TaskInfoDetail> InitDailyTaskDetailList()
    {
        return new List<TaskInfoDetail>
        {
            new() { UserTaskDetail = UserTaskDetail.DailyVote.ToString() },
            new() { UserTaskDetail = UserTaskDetail.DailyFirstInvite.ToString() },
            new() { UserTaskDetail = UserTaskDetail.DailyViewAsset.ToString() }
        };
    }
    
    public static List<TaskInfoDetail> InitExploreTaskDetailList(long completeCount)
    {
        return new List<TaskInfoDetail>
        {
            new() { UserTaskDetail = UserTaskDetail.ExploreJoinTgChannel.ToString() },
            new() { UserTaskDetail = UserTaskDetail.ExploreFollowX.ToString() },
            new() { UserTaskDetail = UserTaskDetail.ExploreJoinDiscord.ToString() },
            new() { UserTaskDetail = UserTaskDetail.ExploreCumulateFiveInvite.ToString(), CompleteCount = completeCount, TaskCount = 5 },
            new() { UserTaskDetail = UserTaskDetail.ExploreCumulateTenInvite.ToString(), CompleteCount = completeCount, TaskCount = 10 },
            new() { UserTaskDetail = UserTaskDetail.ExploreCumulateTwentyInvite.ToString(), CompleteCount = completeCount, TaskCount = 20 }
        };
    }
}