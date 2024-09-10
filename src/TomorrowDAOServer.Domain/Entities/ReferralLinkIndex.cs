using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class ReferralLinkIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Inviter { get; set; }
    [Keyword] public string ReferralLink { get; set; }
    [Keyword] public string ReferralCode { get; set; }
}