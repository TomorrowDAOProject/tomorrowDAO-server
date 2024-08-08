namespace TomorrowDAOServer.Treasury.Dto;

public class GetTreasuryRecordsInput
{
    public string ChainId { get; set; }
    public string TreasuryAddress { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 5;
}