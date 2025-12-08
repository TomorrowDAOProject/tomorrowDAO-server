using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Referral.Dto;

public class SetCycleInput
{
    [Required] public string ChainId { get; set; }
    [Required] public long StartTime { get; set; } 
    [Required] public long EndTime { get; set; }
    [Required] public bool PointsDistribute { get; set; } 
}