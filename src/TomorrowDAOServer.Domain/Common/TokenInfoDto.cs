namespace TomorrowDAOServer.Common;

public class TokenInfoDto
{
    public string Id { get; set; }
    public string ContractAddress { get; set; }
    public string Symbol { get; set; }
    public string ChainId { get; set; }
    public string IssueChainId { get; set; }
    public string TxId { get; set; }
    public string Name { get; set; }
    public string TotalSupply { get; set; }
    public string Supply { get; set; }
    public string Decimals { get; set; }
    public string Holders { get; set; }
    public string Transfers { get; set; }
    public long LastUpdateTime { get; set; }
}