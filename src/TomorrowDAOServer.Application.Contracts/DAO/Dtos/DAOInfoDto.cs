using System;
using System.Collections.Generic;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Governance.Dto;

namespace TomorrowDAOServer.DAO.Dtos;

public class DAOInfoDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
    public string Creator { get; set; }
    public MetadataDto Metadata { get; set; }
    public string GovernanceToken { get; set; }
    public bool IsHighCouncilEnabled { get; set; }
    public string HighCouncilAddress { get; set; }
    public HighCouncilConfigDto HighCouncilConfig { get; set; }
    public GovernanceSchemeThresholdDto GovernanceSchemeThreshold { get; set; } = new();
    public long HighCouncilTermNumber { get; set; }
    public long HighCouncilMemberCount { get; set; }
    public List<FileInfoDto> FileInfoList { get; set; }
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
    public DateTime CreateTime { get; set; }
    public bool IsNetworkDAO { get; set; }

    public void OfGovernanceSchemeThreshold(IndexerGovernanceScheme scheme)
    {
        if (scheme == null)
        {
            return;
        }
        GovernanceSchemeThreshold.MinimalRequiredThreshold = scheme.MinimalRequiredThreshold;
        GovernanceSchemeThreshold.MinimalVoteThreshold = scheme.MinimalVoteThreshold;
        GovernanceSchemeThreshold.MinimalApproveThreshold = scheme.MinimalApproveThreshold;
        GovernanceSchemeThreshold.MaximalRejectionThreshold = scheme.MaximalRejectionThreshold;
        GovernanceSchemeThreshold.MaximalAbstentionThreshold = scheme.MaximalAbstentionThreshold;
        GovernanceSchemeThreshold.ProposalThreshold = scheme.ProposalThreshold;
    }
}

public class MetadataDto
{
    public string Name { get; set; }
    public string LogoUrl { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> SocialMedia { get; set; }
}

public class FileInfoDto
{
    public FileDto File { get; set; }
    public string Uploader { get; set; }
    public DateTime UploadTime { get; set; }
}

public class FileDto
{
    public string Name { get; set; }
    public string Cid { get; set; }
    public string Url { get; set; }
}

public class GovernanceSchemeThresholdDto
{
    public int MinimalRequiredThreshold { get; set; }
    public int MinimalVoteThreshold { get; set; }
    public int MinimalApproveThreshold { get; set; }
    public int MaximalRejectionThreshold { get; set; }
    public int MaximalAbstentionThreshold { get; set; }
    public long ProposalThreshold { get; set; }
}

public class HighCouncilConfigDto
{
    public long MaxHighCouncilMemberCount { get; set; }
    public long MaxHighCouncilCandidateCount { get; set; }
    public long ElectionPeriod { get; set; }
    public long StakingAmount { get; set; }
}

public class PermissionInfoDto
{
    public string Where { get; set; }
    public string What { get; set; }
    public PermissionType PermissionType { get; set; }
    public string Who { get; set; }
}