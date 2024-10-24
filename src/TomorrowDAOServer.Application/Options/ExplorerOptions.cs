using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class ExplorerOptions
{

    public Dictionary<string, string> BaseUrl { get; set; } = new();
    public string BaseUrlV2 { get; set; }

}