using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao;

public class NetworkDaoProposalListIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword]
    public string ChainId { get; set; }
    [Keyword]
    public string OrganizationAddress { get; set; }
    [Keyword]
    public string ProposalId { get; set; }
    [Keyword]
    public NetworkDaoOrgType OrgType { get; set; }
    public string CreatedTxId { get; set; }
    public DateTime CreatedAt { get; set; }
    [Keyword]
    public string Proposer { get; set; }
    [Keyword]
    public string Title { get; set; }
    public string Description { get; set; }
    [Keyword]
    public string ContractAddress { get; set; }
    public string ContractMethod { get; set; }
    public string Code { get; set; }
    public DateTime ExpiredTime { get; set; }
    public Decimal Approvals { get; set; }
    public Decimal Rejections { get; set; }
    public Decimal Abstentions { get; set; }
    [Keyword]
    public NetworkDaoProposalStatusEnum Status { get; set; }
    [Keyword]
    public NetworkDaoCreatedByEnum CreatedBy { get; set; }
    public bool IsContractDeployed { get; set; }
    public string ReleasedTxId { get; set; }
    public string ReleasedTime { get; set; }
}