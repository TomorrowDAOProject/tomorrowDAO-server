using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Governance.Dto;

public class GovernanceSchemeDto
{
    public List<GovernanceScheme> Data { get; set; }
}
public class GovernanceScheme
{
    public string Id { get; set; }
    public string DAOId { get; set; }
    public string SchemeId { get; set; }
    public string SchemeAddress { get; set; }
    public string ChainId { get; set; }
    public string GovernanceMechanism { get; set; }
    public string GovernanceToken { get; set; }
    public DateTime CreateTime { get; set; }
    public long MinimalRequiredThreshold { get; set; }
    public long MinimalVoteThreshold { get; set; }
    public long MinimalApproveThreshold { get; set; }
    public long MaximalRejectionThreshold { get; set; }
    public long MaximalAbstentionThreshold { get; set; }
    public long ProposalThreshold { get; set; }
}