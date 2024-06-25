using System;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal;

namespace TomorrowDAOServer.Entities;

public class ProposalBase : BlockInfoBase
{
    [Keyword] public override string Id { get; set; }
    
    [PropertyName("DAOId")]
    [Keyword] public string DAOId { get; set; }

    [Keyword] public string ProposalId { get; set; }

    [Keyword] public string ProposalTitle { get; set; }
    
    [Keyword] public string ProposalDescription { get; set; }
    
    [Keyword] public string ForumUrl { get; set; }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public ProposalType ProposalType { get; set; }
    
    public DateTime ActiveStartTime { get; set; }
   
    public DateTime ActiveEndTime { get; set; }
    
    public DateTime ExecuteStartTime { get; set; }

    public DateTime ExecuteEndTime { get; set; }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public ProposalStatus ProposalStatus { get; set; }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public ProposalStage ProposalStage { get; set; }
    
    [Keyword] public string Proposer { get; set; }
    
    [Keyword] public string SchemeAddress { get; set; }
    
    public ExecuteTransaction Transaction { get; set; }
    
    [Keyword] public string VoteSchemeId { get; set; }
    
    public VoteMechanism VoteMechanism { get; set; }
    
    [Keyword] public string VetoProposalId { get; set; }
    [Keyword] public string BeVetoProposalId { get; set; }
    
    public DateTime DeployTime { get; set; }

    public DateTime? ExecuteTime { get; set; }   
    
    [JsonConverter(typeof(StringEnumConverter))]
    public GovernanceMechanism GovernanceMechanism { get; set; }
    
    public int MinimalRequiredThreshold { get; set; }
    
    public int MinimalVoteThreshold { get; set; }
    
    //percentage            
    public int MinimalApproveThreshold { get; set; }
    
    //percentage    
    public int MaximalRejectionThreshold { get; set; }
    
    //percentage    
    public int MaximalAbstentionThreshold { get; set; }
    public long ProposalThreshold { get; set; }
    
    public long ActiveTimePeriod { get; set; }
    
    public long VetoActiveTimePeriod { get; set; }
    
    public long PendingTimePeriod { get; set; }
    
    public long ExecuteTimePeriod { get; set; }
    
    public long VetoExecuteTimePeriod { get; set; }

    public bool VoteFinished { get; set; }
    
    public bool IsNetworkDAO { get; set; }
}