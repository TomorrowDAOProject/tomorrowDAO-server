using System.Collections.Generic;

namespace TomorrowDAOServer.NetworkDao.Options;

public class MigratorOptions
{
    public bool QueryExplorerProposal { get; set; } = true;
    //$"{contractAddress}.{contractMethod}"
    public ISet<string> FilterContractMethods { get; set; } = new HashSet<string>();

    public ISet<string> FilterMethods { get; set; } = new HashSet<string>();
}