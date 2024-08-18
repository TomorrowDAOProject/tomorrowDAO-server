using System.Collections.Generic;

namespace TomorrowDAOServer.Ranking.Dto;

public class RankingResultDto
{
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public long CanVoteAmount { get; set; }
    public long TotalVoteAmount { get; set; }
    public List<RankingDetailDto> RankingList { get; set; } = new();
}