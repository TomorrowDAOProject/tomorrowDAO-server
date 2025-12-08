using System;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Vote.Dto;

public class VoteRecordDto
{
    public string Voter { get; set; }
    
    public string TransactionId { get; set; }
    
    public long Amount { get; set; }
    
    public string VotingItemId {get; set; }
    
    public DateTime VoteTime { get; set; }

    public string Option { get; set; }
}