using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class RankingAppUserPointsIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [PropertyName("DAOId")]
    [Keyword] public string DAOId { get; set; }
    [Keyword] public string ProposalId { get; set; }
    [Keyword] public string AppId { get; set; }
    [Keyword] public string Alias { get; set; }
    [Keyword] public string Title { get; set; }
    [Keyword] public string Address { get; set; }
    public long Amount { get; set; }
    public PointsType PointsType { get; set; }
}