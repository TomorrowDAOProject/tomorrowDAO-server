namespace TomorrowDAOServer.Discover.Dto;

public class GetDiscoverAppListInput
{
    public string ChainId { get; set; }
    public string Category { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 8;
}