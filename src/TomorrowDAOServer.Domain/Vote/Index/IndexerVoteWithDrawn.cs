using System;
using System.Collections.Generic;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Vote.Index;

public class IndexerVoteWithdrawn
{
    public List<WithdrawnDto> DataList { get; set; } = new ();
}

public class WithdrawnDto
{
    public string Id { get; set; }
    public long BlockHeight { get; set; }
    public string DaoId { get; set; }
    public string Voter { get; set; }
    public long WithdrawAmount { get; set; }
    public DateTime WithdrawTimestamp { get; set; }
    public List<string> VotingItemIdList { get; set; } = new();
    public DateTime CreateTime { get; set; }
}