using System.Collections.Generic;
using System.Linq;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Ranking.Dto;

public class RankingAppPointsBaseDto
{
    public string ProposalId { get; set; }
    public string Alias { get; set; }
    public long Points { get; set; }
}

public class RankingAppPointsDto : RankingAppPointsBaseDto
{
    public PointsType PointsType { get; set; }
    
    public static List<RankingAppPointsBaseDto> ConvertToBaseList(IEnumerable<RankingAppPointsDto> list)
    {
        return list
            .GroupBy(x => new { x.Alias, x.ProposalId })
            .Select(g => new RankingAppPointsBaseDto
            {
                Alias = g.Key.Alias,
                ProposalId = g.Key.ProposalId,
                Points = g.Sum(x => x.Points)
            })
            .ToList();
    }
}