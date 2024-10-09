using System.Collections.Generic;

namespace TomorrowDAOServer.NetworkDao.Migrator.ES;

public class GetOrgProposerListInput
{
    public string ChainId { get; set; }
    public List<string> OrgAddressList { get; set; }
}