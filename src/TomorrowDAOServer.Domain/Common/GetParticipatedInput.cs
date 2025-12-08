namespace TomorrowDAOServer.Common;

public class GetParticipatedInput
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } 
}