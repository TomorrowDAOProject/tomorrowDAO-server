using System.Collections.Generic;

namespace TomorrowDAOServer.NetworkDao.Migrator.GraphQL;

public class GetProposalReleasedIndexInput : GetNetworkDaoDataInput
{
    public List<string> ProposalIds { get; set; }
    public string Title { get; set; }
}