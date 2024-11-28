using System;
using System.ComponentModel.DataAnnotations;
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

public class GetOrganizationsPagedResult : PagedResultDto<NetworkDaoOrgDto>
{
    
}