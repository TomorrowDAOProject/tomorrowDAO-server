using System.Collections.Generic;
using System.Linq;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Common;

public class TaskPointsHelper
{
    private static readonly Dictionary<UserTask, List<UserTaskDetail>> TaskDetailMapping = new()
    {
        { UserTask.None, new List<UserTaskDetail> { UserTaskDetail.None }},
        { UserTask.Daily, new List<UserTaskDetail>
            {
                UserTaskDetail.DailyViewAds, UserTaskDetail.DailyVote, UserTaskDetail.DailyFirstInvite
            } 
        },
        {
            UserTask.ExploreVotigram, new List<UserTaskDetail>
            {
                UserTaskDetail.ExploreJoinVotigram, UserTaskDetail.ExploreFollowVotigramX, UserTaskDetail.ExploreForwardVotigramX
            }
        },
        {
            UserTask.ExploreApps, new List<UserTaskDetail>()
            {
                UserTaskDetail.ExploreSchrodinger, UserTaskDetail.ExploreJoinTgChannel, UserTaskDetail.ExploreFollowX, UserTaskDetail.ExploreForwardX
            }
        },
        {
            UserTask.Referrals, new List<UserTaskDetail>()
            {
                UserTaskDetail.ExploreCumulateFiveInvite, UserTaskDetail.ExploreCumulateTenInvite, UserTaskDetail.ExploreCumulateTwentyInvite
            }
        }
    };

    public static readonly List<UserTaskDetail> FrontEndTaskDetails = new()
    {
        UserTaskDetail.ExploreJoinVotigram, UserTaskDetail.ExploreFollowVotigramX, UserTaskDetail.ExploreForwardVotigramX,
        UserTaskDetail.ExploreSchrodinger,UserTaskDetail.ExploreJoinTgChannel, UserTaskDetail.ExploreFollowX, UserTaskDetail.ExploreForwardX,
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
            //Daily Tasks
            UserTaskDetail.DailyViewAds => PointsType.DailyViewAds,
            UserTaskDetail.DailyVote => PointsType.Vote,
            UserTaskDetail.DailyFirstInvite => PointsType.DailyFirstInvite,
            //Explore Votigram
            UserTaskDetail.ExploreJoinVotigram => PointsType.ExploreJoinVotigram,
            UserTaskDetail.ExploreFollowVotigramX => PointsType.ExploreFollowVotigramX,
            UserTaskDetail.ExploreForwardVotigramX => PointsType.ExploreForwardVotigramX,
            //Explore Apps
            UserTaskDetail.ExploreSchrodinger => PointsType.ExploreSchrodinger,
            UserTaskDetail.ExploreJoinTgChannel => PointsType.ExploreJoinTgChannel,
            UserTaskDetail.ExploreFollowX => PointsType.ExploreFollowX,
            UserTaskDetail.ExploreForwardX => PointsType.ExploreForwardX,
            //Referrals
            UserTaskDetail.ExploreCumulateFiveInvite => PointsType.ExploreCumulateFiveInvite,
            UserTaskDetail.ExploreCumulateTenInvite => PointsType.ExploreCumulateTenInvite,
            UserTaskDetail.ExploreCumulateTwentyInvite => PointsType.ExploreCumulateTwentyInvite,
            
            //Terminated
            UserTaskDetail.DailyViewAsset => PointsType.DailyViewAsset,
            UserTaskDetail.DailyCreatePoll => PointsType.DailyCreatePoll,
            UserTaskDetail.ExploreJoinDiscord => PointsType.ExploreJoinDiscord,
            _ => null
        };
    }
}