using System.ComponentModel.DataAnnotations;
using Google.Type;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class GetOrganizationsInput : PagedAndSortedResultRequestDto
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public NetworkDaoOrgType ProposalType { get; set; }
    //Org Address
    public string Search { get; set; } = string.Empty;
}

public class GetOrganizationsPagedResult : PagedResultDto<GetOrganizationsResultDto>
{
    
}

public class GetOrganizationsResultDto
{
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public GetProposalListResultDto.ReleaseThresholdDto ReleaseThreshold { get; set; }
    public LeftOrgInfo LeftOrgInfo { get; set; }
    public string OrgAddress { get; set; }
    public string OrgHash { get; set; }
    public string TxId { get; set; }
    public string Creator { get; set; }
    public NetworkDaoOrgType ProposalType { get; set; }
}