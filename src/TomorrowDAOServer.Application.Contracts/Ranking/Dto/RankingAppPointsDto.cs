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
}