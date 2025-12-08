namespace TomorrowDAOServer.Common;

public class GetChainBlockHeightInput
{
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
    public string ChainId { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
}