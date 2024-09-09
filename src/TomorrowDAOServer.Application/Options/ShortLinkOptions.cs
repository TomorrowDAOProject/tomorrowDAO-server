using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class ShortLinkOptions
{
    public Dictionary<string, string> BaseUrl { get; set; } = new();
    public string ProjectCode { get; set; }
}