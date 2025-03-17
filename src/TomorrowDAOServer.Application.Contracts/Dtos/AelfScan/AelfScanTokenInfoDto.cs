namespace TomorrowDAOServer.Dtos.AelfScan;

public class GetTokenInfoFromAelfScanRequest
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
}

public class GetTokenInfoFromAelfScanResponse
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    
    public string Symbol { get; set; }
    public string IssueChainId { get; set; }
    public string TokenName { get; set; }
    public string TotalSupply { get; set; }
    public string Supply { get; set; }
    public string Issued { get; set; }
    public string Issuer { get; set; }
    public string HolderCount { get; set; }
    public string TransferCount { get; set; }
    public string Decimals { get; set; }
    
}