namespace TomorrowDAOServer.Referral.Dto;

public class InviteDetailDto
{
    public long EstimatedReward { get; set; }
    public long AccountCreation { get; set; }
    public long VotigramVote { get; set; }
    public long VotigramActivityVote { get; set; }
    public long EstimatedRewardAll { get; set; }
    public long AccountCreationAll { get; set; }
    public long VotigramVoteAll { get; set; }
    public long VotigramActivityVoteAll { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public bool DuringCycle { get; set; }
    public string Address { get; set; }
    public string CaHash { get; set; }
    public long TotalInvitesNeeded { get; set; }
    public long PointsFirstReferralVote { get; set; }
}