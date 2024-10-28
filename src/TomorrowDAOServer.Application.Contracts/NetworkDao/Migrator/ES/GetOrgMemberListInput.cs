using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.NetworkDao.Migrator.ES;

public class GetOrgMemberListInput : PagedAndSortedResultRequestDto
{
    public string ChainId { get; set; }
    public string OrgAddress { get; set; }
    public List<string> OrgAddresses { get; set; }
    public string MemberAddress { get; set; }
}