using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Ranking.Dto;

public class GetRankingListInput
{
    [Required] public string ChainId { get; set; }
    public string DAOId { get; set; } = string.Empty;
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 6;
}