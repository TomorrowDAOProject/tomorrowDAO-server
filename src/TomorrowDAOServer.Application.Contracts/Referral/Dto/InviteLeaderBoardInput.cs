using System;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Referral.Dto;

public class InviteLeaderBoardInput
{
    [Required] public string ChainId { get; set; }
    [Required] public DateTime StartTime { get; set; }
    [Required] public DateTime EndTime { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 10;
}