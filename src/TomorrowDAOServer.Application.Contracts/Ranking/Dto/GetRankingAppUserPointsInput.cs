using System.Collections.Generic;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Ranking.Dto;

public class GetRankingAppUserPointsInput : PagedResultRequestDto
{
    public string ChainId { get; set; }
    
    public List<PointsType> ExcludePointsTypes { get; set; }
}