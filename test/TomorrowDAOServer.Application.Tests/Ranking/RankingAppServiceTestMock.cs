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
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Dto;
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
            DaoIds = new List<string>{DAOId}, 
            DescriptionBegin = "##GameRanking:", 
            DescriptionPattern = @"^##GameRanking:(?:\s*[a-zA-Z0-9\s\-]+(?:\s*,\s*[a-zA-Z0-9\s\-]+)*)?$",
            LockUserTimeout = 60000,
            VoteTimeout = 60000,
            RetryTimes = 30,
            RetryDelay = 2000
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
        mock.Setup(o => o.GetAndValidateUserAddressAndCaHashAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new Tuple<string, string>(Address1, Address1CaHash));
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

    private IRankingAppPointsRedisProvider MockRankingAppPointsRedisProvider()
    {
        var mock = new Mock<IRankingAppPointsRedisProvider>();
        return mock.Object;
    }
    
    private IUserBalanceProvider MockUserBalanceProvider()
    {
        var mock = new Mock<IUserBalanceProvider>();
        mock.Setup(o => o.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserBalanceIndex{Amount = 1});
        return mock.Object;
    }
    
    private IProposalProvider MockProposalProvider()
    {
        var mock = new Mock<IProposalProvider>();
        mock.Setup(o => o.GetProposalByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ProposalIndex{ActiveStartTime = DateTime.UtcNow, ActiveEndTime = DateTime.UtcNow.AddDays(1)});
        mock.Setup(o => o.GetRankingProposalListAsync(It.IsAny<GetRankingListInput>()))
            .ReturnsAsync(new Tuple<long, List<ProposalIndex>>(1, new List<ProposalIndex> { new() { ProposalId = ProposalId1 } }));
        return mock.Object;
    }
}