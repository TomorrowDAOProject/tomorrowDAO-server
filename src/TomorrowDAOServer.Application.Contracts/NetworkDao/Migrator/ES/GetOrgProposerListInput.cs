using System.Collections.Generic;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.NetworkDao.Migrator.ES;

public class GetOrgProposerListInput : PagedAndSortedResultRequestDto
{
    public string ChainId { get; set; }
    public NetworkDaoOrgType OrgType { get; set; }
    //Proposer Address
    public string Address { get; set; }
    public string OrgAddress { get; set; }
    public List<string> OrgAddresses { get; set; }
}