using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;

namespace TomorrowDAOServer.NetworkDao;

public class NetworkDaoOrgMemberIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string OrgAddress { get; set; }
    [Keyword] public string Member { get; set; }
    [Keyword] public DateTime CreatedAt { get; set; }
}