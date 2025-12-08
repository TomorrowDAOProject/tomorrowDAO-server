using System.Collections.Generic;
using TomorrowDAOServer.Common.Enum;

namespace TomorrowDAOServer.Token.Dto;

public class IssueTokensInput
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }

    public long Amount { get; set; }
    public string ToAddress { get; set; }
    public string Memo { get; set; }
}

public class IssueTokenResponse
{
    public TokenOriginEnum TokenOrigin { get; set; }
    public string Symbol { get; set; }
    public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public long Supply { get; set; }
    public int Decimals { get; set; }
    public string Issuer { get; set; }
    public bool IsBurnable { get; set; }
    public string IssueChainId { get; set; }
    public long Issued { get; set; }
    public string Owner { get; set; }
    public string ProxyAccountHash { get; set; }
    public string ProxyAccountContractAddress { get; set; }
    public string TokenContractAddress { get; set; }
    public string ProxyArgs { get; set; }
    public List<string> RealIssuers { get; set; }
}