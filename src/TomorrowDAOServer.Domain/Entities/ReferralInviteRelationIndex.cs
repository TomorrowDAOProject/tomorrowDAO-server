using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class ReferralInviteRelationIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string InviterCaHash { get; set; }
    [Keyword] public string InviteeCaHash { get; set; }
    [Keyword] public string ReferralCode { get; set; }
    [Keyword] public string ProjectCode { get; set; }
    [Keyword] public string MethodName { get; set; }
    public long Timestamp { get; set; }
    public DateTime? FirstVoteTime { get; set; }
    public bool IsReferralActivity { get; set; }
}