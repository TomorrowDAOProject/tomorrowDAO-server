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

    public long MainChainBlockHeight { get; set; } = 229006413;
    public long SideChainBlockHeigth { get; set; } = 248556545;
    
    //query proposal
    public int RetryCount { get; set; } = 6;
    public int RetryDelay { get; set; } = 10; //s
}