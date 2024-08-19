using System;

namespace TomorrowDAOServer.Ranking.Dto;

public class RankingListDto
{
    public string ChainId { get; set; }
    public string DAOId { get; set; }
    public string ProposalId { get; set; }
    public string ProposalTitle { get; set; }
    public string ProposalDescription { get; set; }
    public long TotalVoteAmount { get; set; }
    public DateTime ActiveStartTime { get; set; }
    public DateTime ActiveEndTime { get; set; }
    public bool Active { get; set; } 
}