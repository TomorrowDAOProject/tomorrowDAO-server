using System;

namespace TomorrowDAOServer.Dtos.Explorer;

public class ExplorerProposalResult
{
    public int Abstentions { get; set; }
    public int Approvals { get; set; }
    public bool CanVote { get; set; }
    public string ContractAddress { get; set; }
    public string ContractMethod { get; set; }
    public DateTime CreateAt { get; set; }
    public string CreateTxId { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ExpiredTime { get; set; }
    public int Id { get; set; }
    public bool IsContractDeployed { get; set; }
    public LeftInfoDto LeftInfo { get; set; }
    public string OrganizationAddress { get; set; }
    public string OrgAddress { get; set; }
    public OrganizationInfoDto OrganizationInfo { get; set; }
    public string ProposalType { get; set; }
    public string TxId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string ProposalId { get; set; }
    public string Proposer { get; set; }
    public int Rejections { get; set; }
    public DateTime ReleasedTime { get; set; }
    public string ReleasedTxId { get; set; }
    public string Status { get; set; }
    public string VotedStatus { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    public class LeftInfoDto
    {
        public string OrganizationAddress { get; set; }
    }

    public class OrganizationInfoDto
    {
        public DateTime CreatedAt { get; set; }
        public string Creator { get; set; }
        public LeftOrgInfo LeftOrgInfo { get; set; }
        public string OrgAddress { get; set; }
        public string OrgHash { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ReleaseThresholdDto ReleaseThreshold { get; set; }
    }


    public class LeftOrgInfo
    {
        public bool ProposerAuthorityRequired { get; set; }
        public bool ParliamentMemberProposingAllowed { get; set; }
        public object CreationToken { get; set; }
    }

    public class ReleaseThresholdDto
    {
        public string MinimalApprovalThreshold { get; set; }
        public string MaximalRejectionThreshold { get; set; }
        public string MaximalAbstentionThreshold { get; set; }
        public string MinimalVoteThreshold { get; set; }
    }
}