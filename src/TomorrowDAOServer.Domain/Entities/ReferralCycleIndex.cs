using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class ReferralCycleIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public bool PointsDistribute { get; set; }
}