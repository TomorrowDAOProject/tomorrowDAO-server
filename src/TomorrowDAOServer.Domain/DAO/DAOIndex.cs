using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.DAO;

public class DAOIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Alias { get; set; }
    [Keyword] public string AliasHexString { get; set; }
    public long BlockHeight { get; set; }
    [Keyword] public string Creator { get; set; }
    public Metadata Metadata { get; set; }
    [Keyword] public string GovernanceToken { get; set; }
    public bool IsHighCouncilEnabled { get; set; }
    [Keyword] public string HighCouncilAddress { get; set; }
    public HighCouncilConfig HighCouncilConfig { get; set; }
    public long HighCouncilTermNumber { get; set; }
    public List<FileInfo> FileInfoList { get; set; }
    public bool IsTreasuryContractNeeded { get; set; }
    public bool SubsistStatus { get; set; }
    [Keyword] public string TreasuryContractAddress { get; set; }
    [Keyword] public string TreasuryAccountAddress { get; set; }
    public bool IsTreasuryPause { get; set; }
    [Keyword] public string TreasuryPauseExecutor { get; set; }
    [Keyword] public string VoteContractAddress { get; set; }
    [Keyword] public string ElectionContractAddress { get; set; }
    [Keyword] public string GovernanceContractAddress { get; set; }
    [Keyword] public string TimelockContractAddress { get; set; }
    public long ActiveTimePeriod { get; set; }
    public long VetoActiveTimePeriod { get; set; }
    public long PendingTimePeriod { get; set; }
    public long ExecuteTimePeriod { get; set; }
    public long VetoExecuteTimePeriod { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsNetworkDAO { get; set; }
    public int VoterCount { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public GovernanceMechanism GovernanceMechanism { get; set; }
}