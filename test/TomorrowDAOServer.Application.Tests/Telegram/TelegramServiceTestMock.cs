using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Moq;
using Nest;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Vote;

namespace TomorrowDAOServer.Telegram;

public partial class TelegramServiceTest
{
    private IOptionsMonitor<TelegramOptions> MockTelegramOptions(out TelegramOptions telegramOptions)
    {
        var mock = new Mock<IOptionsMonitor<TelegramOptions>>();

        telegramOptions = new TelegramOptions
        {
            AllowedCrawlUsers = new HashSet<string>() { Address2 }
        };

        mock.Setup(o => o.CurrentValue).Returns(telegramOptions);

        return mock.Object;
    }

    private INESTRepository<TelegramAppIndex, string> MockTelegramAppIndexRepository()
    {
        var telegramAppIndex = new TelegramAppIndex
        {
            Id = "Id",
            Alias = "Alias",
            Title = "Title",
            Icon = "Icon",
            Description = "Description",
            EditorChoice = true,
            Url = "Url",
            LongDescription = "LongDescription",
            Screenshots = new List<string>() { "Screenshots" },
            Categories = new List<TelegramAppCategory>() { TelegramAppCategory.Earn, TelegramAppCategory.Game },
            CreateTime = DateTime.UtcNow.AddDays(-5),
            UpdateTime = DateTime.UtcNow.AddDays(-5),
            SourceType = SourceType.Telegram,
            Creator = Address1,
            LoadTime = DateTime.UtcNow.AddDays(-5),
            BackIcon = "BackIcon",
            BackScreenshots = new List<string>() { "BackScreenshots" },
            TotalPoints = 10,
            TotalVotes = 10,
            TotalLikes = 10,
            TotalOpenTimes = 1
        };
        var mock = new Mock<INESTRepository<TelegramAppIndex, string>>();
        mock.Setup(o => o.GetListAsync(It.IsAny<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>(),
            It.IsAny<Func<SourceFilterDescriptor<TelegramAppIndex>, ISourceFilter>>(),
            It.IsAny<Expression<Func<TelegramAppIndex, object>>>(), It.IsAny<SortOrder>(), It.IsAny<int>(),
            It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new Tuple<long, List<TelegramAppIndex>>(1,
            new List<TelegramAppIndex>()
            {
                telegramAppIndex
            }));
        mock.Setup(o => o.AddOrUpdateAsync(It.IsAny<TelegramAppIndex>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return mock.Object;
    }
}