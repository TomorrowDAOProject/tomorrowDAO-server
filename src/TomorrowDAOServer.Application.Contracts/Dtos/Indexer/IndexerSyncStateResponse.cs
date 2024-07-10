using System.Collections.Generic;

namespace TomorrowDAOServer.Dtos.Indexer;

public class IndexerSyncStateResponse
{
    public VersionDetail PendingVersion { get; set; }
    public VersionDetail CurrentVersion { get; set; }
}

public class VersionDetail
{
    public string Version { get; set; }
    public List<SyncDetail> Items { get; set; }
}

public class SyncDetail
{
    public string ChainId { get; set; }
    public string LongestChainBlockHash { get; set; }
    public long LongestChainHeight { get; set; }
    public string BestChainBlockHash { get; set; }
    public long BestChainHeight { get; set; }
    public string LastIrreversibleBlockHash { get; set; }
    public long LastIrreversibleBlockHeight { get; set; }
}