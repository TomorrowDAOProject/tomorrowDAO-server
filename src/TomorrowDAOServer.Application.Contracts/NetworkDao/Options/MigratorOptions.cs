using System.Collections.Generic;

namespace TomorrowDAOServer.NetworkDao.Options;

public class MigratorOptions
{
    public bool QueryExplorerProposal { get; set; } = true;
    
    //GraphQL filter
    public List<string> FilterGraphQLToAddresses { get; set; } = new List<string>();
    public List<string> FilterGraphQLMethodNames { get; set; } = new List<string>();

    //local data filter. $"{contractAddress}.{contractMethod}"
    public ISet<string> FilterContractMethods { get; set; } = new HashSet<string>();
    public ISet<string> FilterMethods { get; set; } = new HashSet<string>();
}