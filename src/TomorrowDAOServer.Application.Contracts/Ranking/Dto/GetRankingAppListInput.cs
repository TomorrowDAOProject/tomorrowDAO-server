using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Ranking.Dto;

public class GetRankingAppListInput : PagedResultRequestDto
{
    [Required]
    public string ChainId { get; set; }
    public string Category { get; set; }
    public string Search { get; set; } = string.Empty;
}