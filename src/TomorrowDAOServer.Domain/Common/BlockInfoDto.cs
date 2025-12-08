using System;

namespace TomorrowDAOServer.Common;

public class BlockInfoDto
{
    public string ChainId { get; set; }
    public string? BlockHash { get; set; }
    public long? BlockHeight { get; set; }
    public DateTime? BlockTime { get; set; }
    public string? PreviousBlockHash { get; set; }
    public bool IsDeleted { get; set; }
}