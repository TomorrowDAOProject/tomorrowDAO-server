using System;
using TomorrowDAOServer.Enums;

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
    public long ActiveStartEpochTime { get; set; }
    public long ActiveEndEpochTime { get; set; }
    public bool Active { get; set; } 
    public RankingType RankingType { get; set; } 
    public LabelTypeEnum LabelType { get; set; }
    public string BannerUrl { get; set; }
    public string Proposer { get; set; }
    public string Tag { get; set; }
    public string ProposalType { get; set; } = string.Empty;
    public string ProposerId { get; set; }
    public string ProposerFirstName { get; set; }
}