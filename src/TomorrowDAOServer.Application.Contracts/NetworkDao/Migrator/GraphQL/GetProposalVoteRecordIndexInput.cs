using System.Collections.Generic;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Migrator.GraphQL;

public class GetProposalVoteRecordIndexInput : GetNetworkDaoDataInput
{
    public List<string> ProposalIds { get; set; }
    public NetworkDaoReceiptTypeEnum ReceiptType { get; set; } = NetworkDaoReceiptTypeEnum.All;
}