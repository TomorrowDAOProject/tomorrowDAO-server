using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class ReferralInviteIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Inviter { get; set; }
    [Keyword] public string Invitee { get; set; }
    [Keyword] public string InviteeCaHash { get; set; }
    [Keyword] public string ReferralLink { get; set; }
    [Keyword] public string ReferralCode { get; set; }
    public DateTime? FirstVoteTime { get; set; }
}