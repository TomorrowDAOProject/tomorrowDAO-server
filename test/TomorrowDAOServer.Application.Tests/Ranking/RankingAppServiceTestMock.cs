using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User.Provider;

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
    
    private IUserProvider MockUserProvider()
    {
        var mock = new Mock<IUserProvider>();
        mock.Setup(o => o.GetUserAddressAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(Address1);
        return mock.Object;
    }
    
    private IDAOProvider MockDAOProvider()
    {
        var mock = new Mock<IDAOProvider>();
        mock.Setup(o => o.GetAsync(It.IsAny<GetDAOInfoInput>()))
            .ReturnsAsync(new DAOIndex
            {
                GovernanceToken = ELF
            });
        return mock.Object;
    }
    
    private IRankingAppProvider MockRankingAppProvider()
    {
        var mock = new Mock<IRankingAppProvider>();
        mock.Setup(o => o.GetByProposalIdAsync(It.IsAny<string>(), ProposalId2))
            .ReturnsAsync(new List<RankingAppIndex>
            {
                new()
                {
                    VoteAmount = 1L
                }
            });
        mock.Setup(o => o.GetByProposalIdAsync(It.IsAny<string>(), ProposalId3))
            .ReturnsAsync(new List<RankingAppIndex>
            {
                new()
                {
                    VoteAmount = 1L, ActiveEndTime = DateTime.Now.AddDays(1), ProposalDescription = "##GameRanking:crypto-bot,xrocket,favorite-stickers-bot"
                }
            });
        return mock.Object;
    }
}