using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class CoinGeckoOptions
{
    public const string ClientName = "CoinGeckoPro";
    public string BaseUrl { get; set; }
    public string ApiKey { get; set; }
    public Dictionary<string, string> CoinIdMapping { get; set; }
}

