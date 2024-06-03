namespace TomorrowDAOServer.DAO;

public class GovernanceSchemeThreshold
{
    public int MinimalRequiredThreshold { get; set; }
    public int MinimalVoteThreshold { get; set; }
    public int MinimalApproveThreshold { get; set; }
    public int MaximalRejectionThreshold { get; set; }
    public int MaximalAbstentionThreshold { get; set; }
    public long ProposalThreshold { get; set; }
}