using System.Collections.Generic;

namespace TomorrowDAOServer.NetworkDao.Migrator.ES;

public class GetOrgProposerByOrgAddressInput
{
    public string ChainId { get; set; }
    public List<string> OrgAddressList { get; set; }
}