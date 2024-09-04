using System.Collections.Generic;
using System.Linq;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Ranking.Dto;

public class RankingAppPointsBaseDto
{
    public string ProposalId { get; set; }
    public string Alias { get; set; }
    public long Points { get; set; }
    public double VotePercent { get; set; }
    public double PointsPercent { get; set; }
}

public class RankingAppPointsDto : RankingAppPointsBaseDto
{
    public PointsType PointsType { get; set; }
    
    public static List<RankingAppPointsBaseDto> ConvertToBaseList(List<RankingAppPointsDto> list)
    {
        var votePointsDic = list.Where(x => x.PointsType == PointsType.Vote)
            .ToDictionary(x => x.Alias, x => x.Points);
        var totalPoints = list.Sum(x => x.Points);
        var totalVotePoints = list.Where(x => x.PointsType == PointsType.Vote).Sum(x => x.Points);
        var pointsPercentFactor = DoubleHelper.GetFactor(totalPoints);
        var votePercentFactor = DoubleHelper.GetFactor(totalVotePoints);
        return list
            .GroupBy(x => new { x.Alias, x.ProposalId })
            .Select(g =>
            {
                var key = g.Key;
                var currentPoints = g.Sum(x => x.Points);
                var currentVotePoints = votePointsDic.GetValueOrDefault(key.Alias, 0);
                return new RankingAppPointsBaseDto
                {
                    Alias = key.Alias,
                    ProposalId = key.ProposalId,
                    Points = currentPoints,
                    PointsPercent = currentPoints * pointsPercentFactor,
                    VotePercent = currentVotePoints * votePercentFactor
                };
            })
            .ToList();
    }
}