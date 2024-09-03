using System.Collections.Generic;

namespace TomorrowDAOServer.Ranking.Dto;

public class RankingAppLikeDto
{
    public string ProposalId { get; set; }
    public List<RankingAppLikeDetailDto> LikeList { get; set; }
}

public class RankingAppLikeDetailDto
{
    public string Alias { get; set; }
    public long LikeAmount { get; set; }
}