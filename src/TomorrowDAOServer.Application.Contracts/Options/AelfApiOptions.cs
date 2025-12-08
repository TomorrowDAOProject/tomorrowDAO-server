using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class AelfApiInfoOptions
{
    public Dictionary<string, AelfApiInfo> AelfApiInfos { get; set; }
}

public class AelfApiInfo
{
    public string Domain { get; set; }
}
