using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;

namespace TomorrowDAOServer.ChainFm.Index;

public class ChainFmChannelIndex : AbstractEntity<string>, IIndexBuild
{
    public override string Id { get; set; }
    public string Name { get; set; }
    public string User { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public long Updated_At { get; set; }
    public long Created_At { get; set; }
    public bool Is_Private { get; set; }
    public long Last_Active_At { get; set; }
    public int Follow_Count { get; set; }
    public int Address_Count { get; set; }
}