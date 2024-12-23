using System;
using System.Collections.Generic;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Ranking.Dto;

public class RankingDetailDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long CanVoteAmount { get; set; }
    public long TotalVoteAmount { get; set; }
    public long UserTotalPoints { get; set; }
    public string BannerUrl { get; set; }
    public RankingType RankingType { get; set; } 
    public LabelTypeEnum LabelType { get; set; }
    public string ProposalTitle { get; set; }
    public List<RankingAppDetailDto> RankingList { get; set; } = new();
    public long StartEpochTime { get; set; }
    public long EndEpochTime { get; set; }
}