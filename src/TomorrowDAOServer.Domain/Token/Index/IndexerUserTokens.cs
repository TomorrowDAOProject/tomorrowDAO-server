using System.Collections.Generic;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Token.Index;

public class IndexerUserTokens  : IndexerCommonResult<IndexerUserTokens>
{
    public List<IndexerUserToken> UserTokens { get; set; }
}

public class IndexerUserToken
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string TokenName { get; set; }
    public string ImageUrl { get; set; }
    public int Decimals { get; set; }
    public long Balance { get; set; }
}