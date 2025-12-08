using System.Collections.Generic;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.NetworkDao.Migrator.ES;

public class GetOrgListInput : PagedAndSortedResultRequestDto
{
    public string ChainId { get; set; }
    public NetworkDaoOrgType OrgType { get; set; }
    public string OrgAddress { get; set; }
    public List<string> OrgAddresses { get; set; }
    public bool? ProposerAuthorityRequired { get; set; }
}