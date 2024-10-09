using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class TelegramOptions
{
    public ISet<string> AllowedCrawlUsers { get; set; } = new HashSet<string>();
    public List<string> LoadUrlList { get; set; } = new();
    public string DetailUrl { get; set; }
    public Dictionary<string, string> TgHeader { get; set; } = new();
}