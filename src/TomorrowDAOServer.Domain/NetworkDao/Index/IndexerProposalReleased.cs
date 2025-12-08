using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Index;

public class IndexerProposalReleased  : BlockInfoDto
{
    public string ProposalId { get; set; }
    public string OrganizationAddress { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public NetworkDaoOrgType OrgType { get; set; }
    public TransactionInfoDto TransactionInfo { get; set; }
}