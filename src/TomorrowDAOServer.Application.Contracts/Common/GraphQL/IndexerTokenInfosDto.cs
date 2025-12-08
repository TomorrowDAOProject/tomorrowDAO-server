using System.Collections.Generic;

namespace TomorrowDAOServer.Common.GraphQL;

public class IndexerTokenInfosDto
{
    public IndexerTokenInfoListDto TokenInfo { get; set; }
}

public class IndexerTokenInfoListDto
{
    public long TotalCount  { get; set; }
    public List<IndexerTokenInfoDto> Items { get; set; }
}

public class IndexerTokenInfoDto
{
    public string Symbol { get; set; }
    public long HolderCount { get; set; }
}