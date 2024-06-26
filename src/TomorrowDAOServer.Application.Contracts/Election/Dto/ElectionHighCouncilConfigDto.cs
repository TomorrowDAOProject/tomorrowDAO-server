using System.Collections.Generic;

namespace TomorrowDAOServer.Election.Dto;

public class ElectionHighCouncilConfigDto
{
    public string Id { get; set; }
    public string DaoId { get; set; }
    public long MaxHighCouncilMemberCount { get; set; }
    public long MaxHighCouncilCandidateCount { get; set; }
    public long ElectionPeriod { get; set; }
    public bool IsRequireHighCouncilForExecution { get; set; }
    public string GovernanceToken { get; set; }
    public long StakeThreshold { get; set; }
    public List<string> InitialHighCouncilMembers { get; set; } = new List<string>();

    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public string PreviousBlockHash { get; set; }
    public bool IsDeleted { get; set; }
}

public class GetHighCouncilConfigInput : ElectionGraphqlQueryParams
{
    
}