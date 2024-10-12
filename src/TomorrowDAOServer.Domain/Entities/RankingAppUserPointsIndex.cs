using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class RankingAppUserPointsIndex : AbstractEntity<Guid>, IIndexBuild
{
    [Keyword] public override Guid Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string DAOId { get; set; }
    [Keyword] public string ProposalId { get; set; }
    [Keyword] public string AppId { get; set; }
    [Keyword] public string Alias { get; set; }
    [Keyword] public string Title { get; set; }
    [Keyword] public string Address { get; set; }
    public long Amount { get; set; }
    public long Points { get; set; }
    public PointsType PointsType { get; set; }
    public DateTime UpdateTime { get; set; }
}