namespace TomorrowDAOServer.Enums;

public enum UserTaskDetail
{
    // None
    None = 0,
    
    // Daily
    DailyVote = 1,
    DailyFirstInvite = 2,
    DailyViewAsset = 3,
    DailyViewAds = 11,
    DailyCreatePoll = 12,
    DailyLogin = 17,
    
    // Explore
    ExploreJoinTgChannel = 4,
    ExploreFollowX = 5,
    ExploreJoinDiscord = 6,
    ExploreForwardX = 7,
    ExploreCumulateFiveInvite = 8,
    ExploreCumulateTenInvite = 9,
    ExploreCumulateTwentyInvite = 10,
    ExploreJoinVotigram = 13,
    ExploreFollowVotigramX = 14,
    ExploreForwardVotigramX = 15,
    ExploreSchrodinger = 16
}