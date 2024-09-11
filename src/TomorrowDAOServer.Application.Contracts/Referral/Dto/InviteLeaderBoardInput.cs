using System;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Referral.Dto;

public class InviteLeaderBoardInput
{
    [Required] public string ChainId { get; set; }
    [Required] public long StartTime { get; set; }
    [Required] public long EndTime { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 10;
}