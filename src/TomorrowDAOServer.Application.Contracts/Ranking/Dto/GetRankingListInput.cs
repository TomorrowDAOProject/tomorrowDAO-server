using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Ranking.Dto;

public class GetRankingListInput
{
    [Required] public string ChainId { get; set; }
    public string Type { get; set; } = RankingType.Verified.ToString();
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 6;
}