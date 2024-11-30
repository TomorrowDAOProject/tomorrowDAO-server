using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao;

public class NetworkDaoOrgProposerIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword]
    public string ChainId { get; set; }
    [Keyword] public string OrgAddress { get; set; }
    public string RelatedTxId { get; set; }
    [Keyword] public string Proposer { get; set; }
    [Keyword] public NetworkDaoOrgType OrgType { get; set; }
}