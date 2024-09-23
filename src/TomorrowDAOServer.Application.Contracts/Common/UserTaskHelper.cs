using System.Collections.Generic;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Common;

public class TaskPointsHelper
{
    private static readonly Dictionary<UserTask, List<UserTaskDetail>> TaskDetailMapping = new()
    {
        { UserTask.Daily, new List<UserTaskDetail> { UserTaskDetail.DailyVote, UserTaskDetail.DailyFirstInvite, UserTaskDetail.DailyViewAsset } },
        { UserTask.Explore, new List<UserTaskDetail>
        {
            UserTaskDetail.ExploreJoinTgChannel, UserTaskDetail.ExploreFollowX, UserTaskDetail.ExploreJoinDiscord,
            UserTaskDetail.ExploreCumulateFiveInvite, UserTaskDetail.ExploreCumulateTenInvite, UserTaskDetail.ExploreCumulateTwentyInvite
        } }
    };

    public List<UserTaskDetail> GetUserTaskDetailFromUserTask(UserTask userTask)
    {
        return TaskDetailMapping.GetValueOrDefault(userTask, new List<UserTaskDetail>());
    }
    
    public PointsType? GetPointsTypeFromUserTaskDetail(UserTaskDetail userTaskDetail)
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
}