namespace TomorrowDAOServer.Vote.Dto;

public class GetPageVoteRecordInput
{
    public string ChainId { get; set; }
    
    public string DaoId { get; set; }
    
    public string Voter { get; set; }

    public string VotingItemId { get; set; }
    
    public int SkipCount { get; set; }
    
    public int MaxResultCount { get; set; }
    
    public string VoteOption { get; set; }
    public string Source { get; set; }
}