using System;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Index;

public class IndexerProposal : BlockInfoDto
{
    public string Id { get; set; }
    public string ProposalId { get; set; }
    public string OrganizationAddress { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public NetworkDaoOrgType OrgType { get; set; }
    public bool IsReleased { get; set; }
    public DateTime SaveTime { get; set; }
    public string Symbol { get; set; }
    public long TotalAmount { get; set; }
    public TransactionInfoDto TransactionInfo { get; set; }
}