using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao;

public class NetworkDaoProposalIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword]
    public string ChainId { get; set; }
    [Keyword]
    public string ProposalId { get; set; }
    [Keyword]
    public string OrganizationAddress { get; set; }
    [Keyword]
    public string Title { get; set; }
    public string Description { get; set; }
    [Keyword]
    public NetworkDaoOrgType OrgType { get; set; }
    public bool IsReleased { get; set; }
    public DateTime SaveTime { get; set; }
    public DateTime ExpiredTime { get; set; }
    public string Symbol { get; set; }
    public decimal TotalAmount { get; set; } = 0;
    public decimal Approvals { get; set; } = 0;
    public decimal Rejections { get; set; } = 0;
    public decimal Abstentions { get; set; } = 0;
    
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public DateTime BlockTime { get; set; }
    public string PreviousBlockHash { get; set; }
    public bool IsDeleted { get; set; }
    [Keyword]
    public string Proposer { get; set; }
    public TransactionInfoDto TransactionInfo { get; set; }
    [Keyword]
    public string ContractName { get; set; }
    [Keyword]
    public string ContractAddress { get; set; }
    public string ContractMethod { get; set; }
    public string Code { get; set; }
    public bool IsContractDeployed { get; set; }

    [Keyword] public NetworkDaoProposalStatusEnum Status { get; set; } = NetworkDaoProposalStatusEnum.Pending;
    
    public string ReleasedTxId { get; set; }
    public string ReleasedBlockHeight { get; set; }
    public DateTime ReleasedTime { get; set; }
}