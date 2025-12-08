using System.Collections.Generic;
using TomorrowDAOServer.Ranking.Dto;

namespace TomorrowDAOServer.User.Dtos;

public class GetHomePageInput
{
    public string ChainId { get; set; }
}

public class HomePageResultDto
{
    public long TotalVoteAmount { get; set; }
    public long UserTotalPoints { get; set; }
    public List<RankingAppDetailDto> WeeklyTopVotedApps { get; set; }
    public RankingAppDetailDto DiscoverHiddenGems { get; set; }
}