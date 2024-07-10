using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class IndexerOptions
{
    public Dictionary<string, string> BaseUrl { get; set; } = new();
    public bool UseNewIndexer { get; set; } = false;
}