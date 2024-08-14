using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Telegram.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Spider;

public partial class TelegramAppsSpiderServiceTest
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