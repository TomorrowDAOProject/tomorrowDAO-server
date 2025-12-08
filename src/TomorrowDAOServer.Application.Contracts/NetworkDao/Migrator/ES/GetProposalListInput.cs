using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Dtos;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.NetworkDao.Migrator.ES;

public class GetProposalListInput : PagedResultRequestDto
{
    [Required]
    public string ChainId { get; set; }
    //OrgAddress
    public string Address { get; set; }
    //ProposalId, ContractAddress, Proposer
    public string Search { get; set; }
    public bool? IsContract { get; set; }
    public NetworkDaoProposalStatusEnum Status { get; set; } = NetworkDaoProposalStatusEnum.All;
    public NetworkDaoOrgType ProposalType { get; set; } = NetworkDaoOrgType.Parliament;

    public List<string> ProposalIds { get; set; } = new List<string>();
    public string ProposalId { get; set; }
    public string Proposer { get; set; }
}

public class GetProposalListPageResult : PagedResultDto<GetProposalListResultDto>
{
    public int BpCount { get; set; }
}

public class GetProposalListResultDto
{
    public decimal Abstentions { get; set; }
    public decimal Approvals { get; set; }
    public bool CanVote { get; set; }
    public string ContractAddress { get; set; }
    public string ContractMethod { get; set; }
    public string ContractParams { get; set; }
    public DateTime CreateAt { get; set; }
    public string CreateTxId { get; set; }
    public string CreatedBy { get; set; }
    public DateTime ExpiredTime { get; set; }
    public string Id { get; set; }
    public bool IsContractDeployed { get; set; }
    public LeftInfoDto LeftInfo { get; set; }
    public string OrgAddress { get; set; }
    public NetworkDaoOrgDto OrganizationInfo { get; set; }
    public string ProposalType { get; set; }
    public string TxId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string ProposalId { get; set; }
    public string Proposer { get; set; }
    public decimal Rejections { get; set; }
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
}