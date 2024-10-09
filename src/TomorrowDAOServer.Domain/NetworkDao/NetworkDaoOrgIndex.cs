using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao;

public class NetworkDaoOrgIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string OrgAddress { get; set; }
    [Keyword] public string OrgHash { get; set; }
    [Keyword] public string TxId { get; set; }
    [Keyword] public string Creator { get; set; }
    [Keyword] public NetworkDaoOrgType OrgType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public long MinimalApprovalThreshold { get; set; }
    public long MaximalRejectionThreshold { get; set; }
    public long MaximalAbstentionThreshold { get; set; }
    public long MinimalVoteThreshold { get; set; }

    public bool ParliamentMemberProposingAllowed { get; set; } = false;
    
    public TransactionInfoDto TransactionInfo { get; set; }
}