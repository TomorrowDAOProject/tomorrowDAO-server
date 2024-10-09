using System.Collections.Generic;

namespace TomorrowDAOServer.NetworkDao.Migrator.GraphQL;

public class GetProposalIndexInput : GetNetworkDaoDataInput
{
    public List<string> ProposalIds { get; set; }
    public string Title { get; set; }
}