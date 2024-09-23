namespace TomorrowDAOServer.Enums;

public enum PointsType
{
    All = 0,
    
    // normal
    Vote = 1, // both for normal and daily task
    Like = 2,
    
    // referral activity
    InviteVote = 3, // inviter get points when invitee register and vote for first time during referral activity period
    BeInviteVote = 4, // invitee get points when invitee register and vote for first time during referral activity period
    
    // daily task
    DailyViewAsset = 5,
    DailyFirstInvite = 6,
    
    // explore task
    ExploreJoinTgChannel = 7,
    ExploreFollowX = 8,
    ExploreJoinDiscord = 9,
    ExploreCumulateFiveInvite = 10,
    ExploreCumulateTenInvite = 11,
    ExploreCumulateTwentyInvite = 12
}