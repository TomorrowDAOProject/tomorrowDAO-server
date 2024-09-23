using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class UserPointsRecordIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
    public PointsType PointsType { get; set; }
    public long Points { get; set; }
    public string Information { get; set; }
    public DateTime PointsTime { get; set; }
}