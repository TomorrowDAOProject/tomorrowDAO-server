using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class HubCommonOptions
{
    public Dictionary<string, int> DelayMaps { get; set; } 
    
    public int GetDelay(string key)
    {
        return DelayMaps.GetValueOrDefault(key, 1000);
    }
}