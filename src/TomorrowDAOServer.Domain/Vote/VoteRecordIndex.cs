using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Vote;

public class VoteRecordIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public long BlockHeight { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string DAOId { get; set; }
    // The voting activity id.(proposal id)
    [Keyword] public string VotingItemId { get; set; }
    [Keyword] public string Voter { get; set; }
    [Keyword] public VoteMechanism VoteMechanism { get; set; }
    public long Amount { get; set; }
    public VoteOption Option { get; set; }
    public DateTime VoteTime { get; set; }
    public bool IsWithdraw { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Memo { get; set; }
    public bool ValidRankingVote { get; set; }
    public string Alias { get; set; }
    public string Title { get; set; }
}