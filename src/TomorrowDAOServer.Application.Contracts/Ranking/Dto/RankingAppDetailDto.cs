using System;

namespace TomorrowDAOServer.Ranking.Dto;

public class RankingAppDetailDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string DAOId { get; set; }
    public string ProposalId { get; set; }
    public string ProposalTitle { get; set; }
    public string ProposalDescription { get; set; }
    public DateTime ActiveStartTime { get; set; }
    public DateTime ActiveEndTime { get; set; }
    public string AppId { get; set; }
    public string Alias { get; set; }
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public bool EditorChoice { get; set; }
    public DateTime DeployTime { get; set; }
    public long VoteAmount { get; set; }
    public double VotePercent { get; set; }
}