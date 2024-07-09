using System;
using System.Collections.Generic;
using TomorrowDAOServer.Common.Enum;
using ProposalType = TomorrowDAOServer.Common.Enum.ProposalType;

namespace TomorrowDAOServer.Dtos.Explorer;

public class ExplorerProposalInfoRequest
{
    public ExplorerProposalInfo Proposal { get; set; }
    public List<string> BpList { get; set; }
    public List<string> ParliamentProposerList { get; set; }
}

public class ExplorerProposalInfoResponse
{
    public string ProposalId { get; set; }
}

public class ExplorerProposalInfo
{
    public string OrgAddress { get; set; }
    public string CreateTxId { get; set; }
    public DateTime CreateAt { get; set; }
    public string ProposalId { get; set; }
    public string Proposer { get; set; }
    public string ContractAddress { get; set; }
    public string ContractMethod { get; set; }
    public string ContractParams { get; set; }
    public DateTime ExpiredTime { get; set; }
    public Decimal Approvals { get; set; }
    public Decimal Rejections { get; set; }
    public Decimal Abstentions { get; set; }
    public Dictionary<string, object> LeftInfo { get; set; }
    public ExplorerProposalStatusEnum Status { get; set; }
    public string ReleasedTxId { get; set; }
    public DateTime ReleasedTime { get; set; }
    public ExplorerCreatedByTypeEnum CreatedBy { get; set; }
    public bool IsContractDeployed { get; set; }
    public ProposalType ProposalType { get; set; }
}