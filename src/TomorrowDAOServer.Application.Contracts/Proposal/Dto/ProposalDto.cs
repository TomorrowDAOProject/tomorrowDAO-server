using System;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Proposal.Dto;

public class ProposalBasicDto
{
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
    public string Id { get; set; }

    public string DAOId { get; set; }
    
    public string Alias { get; set; }

    public string ProposalId { get; set; }

    public string ProposalTitle { get; set; }

    public string ProposalDescription { get; set; }

    public string ForumUrl { get; set; }

    public string ProposalType { get; set; }

    public DateTime ActiveStartTime { get; set; }

    public DateTime ActiveEndTime { get; set; }

    public DateTime? ExecuteStartTime { get; set; }

    public DateTime? ExecuteEndTime { get; set; }

    public string ProposalStatus { get; set; }
    public string RealProposalStatus { get; set; }

    public string ProposalStage { get; set; }
    
    public string ProposalStatusForOnChain { get; set; }

    public string Proposer { get; set; }

    public string SchemeAddress { get; set; }

    public ExecuteTransaction Transaction { get; set; }

    public string VoteSchemeId { get; set; }
    
    public string VetoProposalId { get; set; }

    public DateTime DeployTime { get; set; }

    public DateTime? ExecuteTime { get; set; }

    public string GovernanceMechanism { get; set; }

    public long MinimalRequiredThreshold { get; set; }

    public long MinimalVoteThreshold { get; set; }

    //percentage            
    public long MinimalApproveThreshold { get; set; }

    //percentage    
    public long MaximalRejectionThreshold { get; set; }

    //percentage    
    public long MaximalAbstentionThreshold { get; set; }
    
    public ProposalSourceEnum ProposalSource { get; set; }
    public ProposalCategory ProposalCategory { get; set; }
}

public class ProposalDto : ProposalBasicDto
{
    public string VoteMechanismName { get; set; }

    //vote count info
    public long VoterCount { get; set; }

    public long VotesAmount { get; set; }

    public long ApprovedCount { get; set; }

    public long RejectionCount { get; set; }

    public long AbstentionCount { get; set; }

    public string Decimals { get; set; }

    public string Symbol { get; set; }
}