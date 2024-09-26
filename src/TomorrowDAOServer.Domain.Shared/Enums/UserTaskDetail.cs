namespace TomorrowDAOServer.Enums;

public enum UserTaskDetail
{
    // None
    None = 0,
    
    // Daily
    DailyVote = 1,
    DailyFirstInvite = 2,
    DailyViewAsset = 3,
    
    // Explore
    ExploreJoinTgChannel = 4,
    ExploreFollowX = 5,
    ExploreJoinDiscord = 6,
    ExploreCumulateFiveInvite = 7,
    ExploreCumulateTenInvite = 8,
    ExploreCumulateTwentyInvite = 9
}