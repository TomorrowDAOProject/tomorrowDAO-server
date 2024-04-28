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
    public int MinimalRequiredThreshold { get; set; }
    public int MinimalVoteThreshold { get; set; }
    public int MinimalApproveThreshold { get; set; }
    public int MaximalRejectionThreshold { get; set; }
    public int MaximalAbstentionThreshold { get; set; }
}