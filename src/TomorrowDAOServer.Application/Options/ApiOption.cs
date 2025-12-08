using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class ApiOption
{
    public Dictionary<string, string> ChainNodeApis { get; set; } = new();
}