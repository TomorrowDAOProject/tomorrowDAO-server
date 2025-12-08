using System;
using System.Collections.Generic;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.DAO.Indexer;

public class IndexerDAOInfos : IndexerCommonResult<IndexerDAOInfos>
{
    public List<IndexerDAOInfo> DAOInfos { get; set; }
}

public class IndexerDAOInfo
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
    public string Creator { get; set; }
    public IndexerMetadata Metadata { get; set; }
    public string GovernanceToken { get; set; }
    public bool IsHighCouncilEnabled { get; set; }
    public string HighCouncilAddress { get; set; }
    public long MaxHighCouncilMemberCount { get; set; }
    public long MaxHighCouncilCandidateCount { get; set; }
    public long ElectionPeriod { get; set; }
    public long StakingAmount { get; set; }
    public long HighCouncilTermNumber { get; set; }
    public string FileInfoList { get; set; }
    public bool IsTreasuryContractNeeded { get; set; }
    public bool SubsistStatus { get; set; }
    public string TreasuryContractAddress { get; set; }
    public string TreasuryAccountAddress { get; set; }
    public bool IsTreasuryPause { get; set; }
    public string TreasuryPauseExecutor { get; set; }
    public string VoteContractAddress { get; set; }
    public string ElectionContractAddress { get; set; }
    public string GovernanceContractAddress { get; set; }
    public string TimelockContractAddress { get; set; }
    public string PermissionAddress { get; set; }
    public string PermissionInfoList { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsNetworkDAO { get; set; }
    public int VoterCount { get; set; }
    public GovernanceMechanism GovernanceMechanism { get; set; }
}

public class IndexerMetadata
{
    public string Name { get; set; }
    public string LogoUrl { get; set; }
    public string Description { get; set; }
    public string SocialMedia { get; set; }
}