namespace TomorrowDAOServer.Referral.Dto;

public class InviteLeaderBoardDto
{
    public long Rank { get; set; }
    public string Inviter { get; set; }
    public string InviterCaHash { get; set; }
    public long InviteAndVoteCount { get; set; }
}