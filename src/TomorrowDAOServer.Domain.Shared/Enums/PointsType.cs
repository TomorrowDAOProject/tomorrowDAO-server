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
    TopInviter = 5,
    
    // daily task
    DailyViewAsset = 6,
    DailyFirstInvite = 7,
    
    // explore task
    ExploreJoinTgChannel = 8,
    ExploreFollowX = 9,
    ExploreJoinDiscord = 10,
    ExploreCumulateFiveInvite = 11,
    ExploreCumulateTenInvite = 12,
    ExploreCumulateTwentyInvite = 13
}