using System;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Referral.Dto;

public class InviteLeaderBoardInput
{
    [Required] public string ChainId { get; set; }
    public long StartTime { get; set; } = 0;
    public long EndTime { get; set; } = 0;
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 10;
}