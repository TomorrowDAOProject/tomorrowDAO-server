using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class TelegramOptions
{
    public ISet<string> AllowedCrawlUsers { get; set; } = new HashSet<string>();
}