using System;
using System.Collections.Generic;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Vote.Index;

public class IndexerVoteStakes : IndexerCommonResult<IndexerVoteStakes>
{
    public List<IndexerVote> DataList { get; set; } = new ();
}

public class IndexerVoteStake : IndexerCommonResult<IndexerVoteStake>
{
    // The voting activity id.(proposal id/customize)
    public string VotingItemId { get; set; }

    public string Voter { get; set; }

    public string AcceptedCurrency { get; set; }
    public long Amount { get; set; }

    public DateTime CreateTime { get; set; }
}