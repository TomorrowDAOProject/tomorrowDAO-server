using Nest;

namespace TomorrowDAOServer.Entities;

public class BlockInfoBase : AbstractEntity<string>
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string BlockHash { get; set; }
    [Keyword] public long BlockHeight { get; set; }
}