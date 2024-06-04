using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class DaoOption
{
    public ISet<string> FilteredDaoNames { get; set; } = new HashSet<string>();
    public List<string> TopDaoNames { get; set; } = new();
}