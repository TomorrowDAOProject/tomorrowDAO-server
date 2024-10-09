using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Index;

public class IndexerOrgChanged : BlockInfoDto
{
    public string OrganizationAddress { get; set; }
    public NetworkDaoOrgType OrgType { get; set; }
    public TransactionInfoDto TransactionInfo { get; set; }
}