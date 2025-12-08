namespace TomorrowDAOServer.Grains.State.Token;

[GenerateSerializer]
public class ExplorerTokenState
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string ContractAddress { get; set; }
    [Id(2)] public string Symbol { get; set; }
    [Id(3)] public string ChainId { get; set; }
    [Id(4)] public string IssueChainId { get; set; }
    [Id(5)] public string TxId { get; set; }
    [Id(6)] public string Name { get; set; }
    [Id(7)] public string TotalSupply { get; set; }
    [Id(8)] public string Supply { get; set; }
    [Id(9)] public string Decimals { get; set; }
    [Id(10)] public string Holders { get; set; }
    [Id(11)] public string Transfers { get; set; }
    [Id(12)] public long LastUpdateTime { get; set; }
}