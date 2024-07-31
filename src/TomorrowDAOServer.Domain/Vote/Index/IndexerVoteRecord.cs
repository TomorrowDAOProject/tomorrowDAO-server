using System;
using System.Collections.Generic;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Vote.Index;

public class IndexerVoteRecords
{
    public List<IndexerVoteRecord> DataList { get; set; } = new ();
}
public class IndexerVoteRecord
{
    public string Id { get; set; }
    public long BlockHeight { get; set; }
    public string ChainId { get; set; }
    public string Voter { get; set; }
    public string TransactionId { get; set; }
    public string DAOId { get; set; }
    public VoteMechanism VoteMechanism { get; set; }
    public long Amount { get; set; }
    public string VotingItemId {get; set; }

    public VoteOption Option { get; set; }
    public DateTime VoteTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}