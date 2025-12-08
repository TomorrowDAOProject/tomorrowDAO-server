using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Ranking.Dto;

public class GetRankingListInput : PagedResultRequestDto
{
    [Required] public string ChainId { get; set; }
    public RankingType Type { get; set; } = RankingType.All;
}