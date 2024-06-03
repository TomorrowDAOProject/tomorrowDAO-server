using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class TestDaoOption
{
    public ISet<string> FilteredDaoNames { get; set; } = new HashSet<string>();
}