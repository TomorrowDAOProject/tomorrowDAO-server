using System.Collections.Generic;

namespace TomorrowDAOServer.Ranking.Dto;

public class RankingActivityResultDto
{
    public List<RankingActivityUserInfotDto> Data { get; set; }
}

public class RankingActivityUserInfotDto
{
    public string Address { get; set; }
    public long Points { get; set; }
    public long Rank { get; set; }
}