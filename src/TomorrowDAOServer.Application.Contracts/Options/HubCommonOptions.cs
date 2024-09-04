using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class HubCommonOptions
{
    public Dictionary<string, int> DelayMaps { get; set; } = new();
    
    public int GetDelay(string key)
    {
        return DelayMaps.GetValueOrDefault(key, 1000);
    }
}