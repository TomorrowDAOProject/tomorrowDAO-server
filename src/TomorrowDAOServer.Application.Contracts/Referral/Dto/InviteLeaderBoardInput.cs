using System;

namespace TomorrowDAOServer.Referral.Dto;

public class InviteLeaderBoardInput
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 10;
}