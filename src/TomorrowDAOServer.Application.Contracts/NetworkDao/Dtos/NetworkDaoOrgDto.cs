using System;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Migrator.ES;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class NetworkDaoOrgDto
{
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public NetworkDaoOrgReleaseThresholdDto ReleaseThreshold { get; set; }
    public NetworkDaoOrgLeftOrgInfoDto NetworkDaoOrgLeftOrgInfoDto { get; set; }
    public string OrgAddress { get; set; }
    public string OrgHash { get; set; }
    public string TxId { get; set; }
    public string Creator { get; set; }
    public NetworkDaoOrgType ProposalType { get; set; }
}

public class NetworkDaoOrgReleaseThresholdDto
{
    public string MinimalApprovalThreshold { get; set; }
    public string MaximalRejectionThreshold { get; set; }
    public string MaximalAbstentionThreshold { get; set; }
    public string MinimalVoteThreshold { get; set; }
}

public class NetworkDaoOrgLeftOrgInfoDto
{
    public string TokenSymbol { get; set; }
    public bool ProposerAuthorityRequired { get; set; }
    public bool ParliamentMemberProposingAllowed { get; set; }
    public OrganizationMemberList OrganizationMemberList { get; set; }
    public ProposerWhiteList ProposerWhiteList { get; set; }
    public string CreationToken { get; set; }
}