namespace TomorrowDAOServer.Referral.Dto;

public class InviteDetailDto
{
    public long EstimatedReward { get; set; }
    public long AccountCreation { get; set; }
    public long VotigramVote { get; set; }
    public long VotigramActivityVote { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public bool DuringCycle { get; set; }
}