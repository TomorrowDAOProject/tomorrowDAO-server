using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class HubCommonOptions
{
    public Dictionary<string, int> DelayMaps { get; set; } = new();
    // todo remove
    public bool Mock { get; set; }
    public string MockProposalId { get; set; }
    public string AliasListString { get; set; }
    
    public int GetDelay(string key)
    {
        return DelayMaps.GetValueOrDefault(key, 1000);
    }
}