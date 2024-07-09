using System.Collections.Generic;

namespace TomorrowDAOServer.NetworkDao.Dto;

public class ProposalListRequest
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Search { get; set; }
    public int IsContract { get; set; }
    public int PageSize { get; set; }
    public int PageNum { get; set; }
    public string Status { get; set; }
    public string ProposalType { get; set; }
}

public class ProposalListResponse
{
    public string ChainId { get; set; }
    public string ProposalId { get; set; }
    public string Proposer { get; set; }
    public string DeployTime { get; set; }
    public string ProposalTitle { get; set; }
    public string GovernanceType { get; set; }
    public string ProposalStatus { get; set; }
    public string ProposalDescription { get; set; }
    public string ProposalType { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public string ExpiredTime { get; set; }
    public TransactionDto Transaction { get; set; }
    public string TotalVoteCount { get; set; }
    public string ApprovedCount { get; set; }
    public string RejectionCount { get; set; }
    public string AbstentionCount { get; set; }
    public int MinimalRequiredThreshold { get; set; }
    public int MinimalVoteThreshold { get; set; }
    public int MinimalApproveThreshold { get; set; }
    public int MaximalRejectionThreshold { get; set; }
    public int MaximalAbstentionThreshold { get; set; }
    public List<string> TagList { get; set; }

    public class TransactionDto
    {
        public string ContractMethodName { get; set; }
        public string ToAddress { get; set; }
    }
}