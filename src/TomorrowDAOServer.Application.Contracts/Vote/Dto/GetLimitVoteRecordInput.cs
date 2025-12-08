namespace TomorrowDAOServer.Vote.Dto;

public class GetLimitVoteRecordInput
{
    public string ChainId { get; set; }

    public string VotingItemId { get; set; }
    
    public string Voter { get; set; }
    
    public string Sorting { get; set; }

    public int Limit { get; set; } = 100;
}