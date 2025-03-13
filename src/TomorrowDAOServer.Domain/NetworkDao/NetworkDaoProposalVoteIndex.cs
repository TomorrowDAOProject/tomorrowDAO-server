using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao;

public class NetworkDaoProposalVoteIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword]
    public override string Id { get; set; }
    [Keyword]
    public string ProposalId { get; set; }
    [Keyword]
    public string Address { get; set; }
    [Keyword]
    //Approve, Reject or Abstain
    public NetworkDaoReceiptTypeEnum ReceiptType { get; set; }
    public DateTime Time { get; set; }
    [Keyword]
    public string OrganizationAddress { get; set; }
    [Keyword]
    public NetworkDaoOrgType OrgType { get; set; }
    public string Symbol { get; set; }
    public long Amount { get; set; }
    [Keyword]
    public string ChainId { get; set; }
    public string? BlockHash { get; set; }
    public long? BlockHeight { get; set; }
    public DateTime? BlockTime { get; set; }
    public string? PreviousBlockHash { get; set; }
    public bool IsDeleted { get; set; }
    public TransactionInfoDto TransactionInfo { get; set; }
}