using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class TelegramOptions
{
    public ISet<string> AllowedCrawlUsers { get; set; } = new HashSet<string>();
    public List<string> LoadUrlList { get; set; } = new();
    public string DetailUrl { get; set; }
    public Dictionary<string, string> TgHeader { get; set; } = new();
    public string Types { get; set; }
    public List<string> FindMiniCategoryList { get; set; } = new();
    public int TgSpiderTime { get; set; } = 12;
    public int FindminiSpiderTime { get; set; } = 12;
}