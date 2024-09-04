using System.Collections.Generic;

namespace TomorrowDAOServer.Ranking.Dto;

public class GetAllAppPointsInput
{
    public string ChainId { get; set; }
    public string ProposalId { get; set; }
    public List<string> AliasList { get; set; } = new();
}