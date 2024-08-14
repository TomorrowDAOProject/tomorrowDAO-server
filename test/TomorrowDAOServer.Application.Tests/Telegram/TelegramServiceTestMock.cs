using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Moq;
using TomorrowDAOServer.Options;

namespace TomorrowDAOServer.Telegram;

public partial class TelegramServiceTest
{
    private IOptionsMonitor<TelegramOptions> MockTelegramOptions()
    {
        var mock = new Mock<IOptionsMonitor<TelegramOptions>>();

        mock.Setup(o => o.CurrentValue).Returns(new TelegramOptions
        {
            AllowedCrawlUsers = new HashSet<string>() { Address2 }
        });

        return mock.Object;
    }
}