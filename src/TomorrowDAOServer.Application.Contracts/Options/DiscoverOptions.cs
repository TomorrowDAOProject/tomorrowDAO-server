using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class DiscoverOptions
{
    public List<string> TopApps { get; set; } = new();
    public List<string> AdUrls { get; set; } = new();
}