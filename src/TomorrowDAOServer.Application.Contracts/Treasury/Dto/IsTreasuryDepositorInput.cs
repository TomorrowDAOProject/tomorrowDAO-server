namespace TomorrowDAOServer.Treasury.Dto;

public class IsTreasuryDepositorInput
{
    public string ChainId { get; set; }
    public string TreasuryAddress { get; set; }
    public string Address { get; set; }
    
    public string GovernanceToken { get; set; }
}