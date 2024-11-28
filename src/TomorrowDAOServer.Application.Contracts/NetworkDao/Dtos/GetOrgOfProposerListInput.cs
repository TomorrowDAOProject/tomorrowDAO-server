using System;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class GetOrgOfProposerListInput : PagedAndSortedResultRequestDto
{
    [Required]
    public string ChainId { get; set; }
    //User Address
    [Required]
    public string Address { get; set; }
    [Required]
    public NetworkDaoOrgType ProposalType { get; set; }
    //Org Address
    public string Search { get; set; }
}

public class GetOrgOfProposerListPagedResult : PagedResultDto<GetOrgOfProposerListResultDto>
{
    
}

public class GetOrgOfProposerListResultDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public NetworkDaoOrgReleaseThresholdDto ReleaseThreshold { get; set; }
    public NetworkDaoOrgLeftOrgInfoDto LeftOrgInfo { get; set; }
    public string OrgAddress { get; set; }
    public string OrgHash { get; set; }
    public string TxId { get; set; }
    public string Creator { get; set; }
    public NetworkDaoOrgType ProposalType { get; set; }
}