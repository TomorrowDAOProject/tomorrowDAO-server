using System.Collections.Generic;

namespace TomorrowDAOServer.Vote.Index;

public class IndexerVotes
{
    public List<IndexerVote> Data { get; set; } = new ();
}
public class IndexerVote
{
    // The voting activity id.(proposal id/customize)
    public string VotingItemId { get; set; }
    public string Executer { get; set; }

    public string VoteSchemeId { get; set; }

    public string DAOId { get; set; }

    public string AcceptedCurrency { get; set; }

    public long ApprovedCount { get; set; }

    public long RejectionCount { get; set; }

    public long AbstentionCount { get; set; }

    public long VotesAmount { get; set; }
    
    public long VoterCount { get; set; }
}