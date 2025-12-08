using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Proposal.Dto;

public class QueryProposalByProposerRequest
{
    public string ChainId { get; set; }
    public string DaoId { get; set; }
    public ProposalStatus ProposalStatus { get; set; }
    public ProposalStage ProposalStage { get; set; }
    public string Proposer { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}