using System;
using System.Collections.Generic;

namespace TomorrowDAOServer.Ranking.Dto;

public class RankingDetailDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long CanVoteAmount { get; set; }
    public long TotalVoteAmount { get; set; }
    public long UserTotalPoints { get; set; }
    public List<RankingAppDetailDto> RankingList { get; set; } = new();
}