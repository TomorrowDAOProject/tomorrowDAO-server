using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;

namespace TomorrowDAOServer.Ranking;

public partial class RankingAppServiceTest
{
    private IOptionsMonitor<RankingOptions> MockRankingOptions()
    {
        var mock = new Mock<IOptionsMonitor<RankingOptions>>();

        mock.Setup(o => o.CurrentValue).Returns(new RankingOptions
        {
            DaoIds = new List<string>{DAOId}, DescriptionBegin = "##GameRanking:", DescriptionPattern = @"^##GameRanking:(?:\s*[a-zA-Z0-9\s\-]+(?:\s*,\s*[a-zA-Z0-9\s\-]+)*)?$"
        });

        return mock.Object;
    }
    
    private ITelegramAppsProvider MockTelegramAppsProvider()
    {
        var mock = new Mock<ITelegramAppsProvider>();
        mock.Setup(o => o.GetTelegramAppsAsync(It.IsAny<QueryTelegramAppsInput>()))
            .ReturnsAsync(new Tuple<long, List<TelegramAppIndex>>(1L, new List<TelegramAppIndex>{new() {Id = "id" }}));
        return mock.Object;
    }
}