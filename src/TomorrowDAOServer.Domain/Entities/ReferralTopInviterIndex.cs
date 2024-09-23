using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class ReferralTopInviterIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string InviterCaHash { get; set; }
    [Keyword] public string InviterAddress { get; set; }
    [Keyword] public string ReferralCode { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public long Rank { get; set; }
    public long InviterCount { get; set; }
    public long Points { get; set; }
    public DateTime CreateTime { get; set; }
}