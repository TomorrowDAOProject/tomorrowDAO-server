using System;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Index;

public class IndexerProposalVoteRecord : BlockInfoDto
{
    public string Id { get; set; }
    public string ProposalId { get; set; }
    public string Address { get; set; }
    //Approve, Reject or Abstain
    public NetworkDaoReceiptTypeEnum ReceiptType { get; set; }
    public DateTime Time { get; set; }
    public string OrganizationAddress { get; set; }
    public NetworkDaoOrgType OrgType { get; set; }
    public string Symbol { get; set; }
    public long Amount { get; set; }
    public TransactionInfoDto TransactionInfo { get; set; }
}