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
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User.Provider;
using TomorrowDAOServer.Vote;
using TomorrowDAOServer.Vote.Provider;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Ranking;

public partial class RankingAppServiceTest
{
    private IOptionsMonitor<RankingOptions> MockRankingOptions()
    {
        var mock = new Mock<IOptionsMonitor<RankingOptions>>();

        mock.Setup(o => o.CurrentValue).Returns(new RankingOptions
        {
            DaoIds = new List<string> { DAOId },
            DescriptionBegin = "##GameRanking:",
            DescriptionPattern = @"^##GameRanking:(?:\s*[a-zA-Z0-9\s\-]+(?:\s*,\s*[a-zA-Z0-9\s\-]+)*)?$",
            LockUserTimeout = 60000,
            VoteTimeout = 60000,
            RetryTimes = 30,
            RetryDelay = 2000,
            ReferralPointsAddressList = new List<string>() { "item1", "item2" }
        });

        return mock.Object;
    }

    private ITelegramAppsProvider MockTelegramAppsProvider()
    {
        var mock = new Mock<ITelegramAppsProvider>();
        var telegramApp1 = new TelegramAppIndex
        {
            Id = "id",
            Alias = "crypto-bot",
            Title = "crypto-bot",
            Icon = "icon",
            Description = "description",
            EditorChoice = true,
            Url = "url",
            LongDescription = "longDescription",
            Screenshots = new List<string>(),
            Categories = null,
            CreateTime = DateTime.UtcNow.Date.AddDays(-30),
            UpdateTime = DateTime.UtcNow.Date.AddDays(-30),
            SourceType = SourceType.Telegram,
            Creator = Address1,
            LoadTime = default,
            BackIcon = null,
            BackScreenshots = null,
            TotalPoints = 0,
            TotalVotes = 0,
            TotalLikes = 0,
            TotalOpenTimes = 0
        };
        var telegramAppList1 = new List<TelegramAppIndex>
        {
            telegramApp1
        };
        mock.Setup(o => o.GetTelegramAppsAsync(It.IsAny<QueryTelegramAppsInput>(), It.IsAny<bool>()))
            .ReturnsAsync(new Tuple<long, List<TelegramAppIndex>>(1L, telegramAppList1));
        mock.Setup(o => o.GetTelegramAppsAsync(It.IsAny<QueryTelegramAppsInput>(), It.IsAny<bool>()))
            .ReturnsAsync(new Tuple<long, List<TelegramAppIndex>>(1, telegramAppList1));
        mock.Setup(o => o.GetLatestCreatedAsync()).ReturnsAsync(telegramApp1);
        mock.Setup(o => o.GetAllByTimePeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TelegramAppIndex>() { telegramApp1 });
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
        var rankingAppIndex = new RankingAppIndex
        {
            Id = "id",
            ChainId = ChainIdAELF,
            DAOId = DaoId,
            ProposalId = ProposalId1,
            ProposalTitle = "ProposalTitle",
            VoteAmount = 1L,
            Url = "Url",
            LongDescription = null,
            Screenshots = new List<string>(),
            TotalPoints = 0,
            TotalVotes = 0,
            TotalLikes = 0,
            Categories = null,
            AppIndex = 0,
            ActiveEndTime = DateTime.Now.AddDays(1),
            AppId = null,
            Alias = "Alias",
            Title = null,
            Icon = null,
            Description = null,
            EditorChoice = false,
            DeployTime = DateTime.Now.AddDays(1),
            ProposalDescription = "##GameRanking:crypto-bot,xrocket,favorite-stickers-bot",
            ActiveStartTime =  DateTime.Now.AddDays(1)
        };
        mock.Setup(o => o.GetByProposalIdAsync(It.IsAny<string>(), ProposalId2))
            .ReturnsAsync(new List<RankingAppIndex>
            {
                rankingAppIndex
            });
        mock.Setup(o => o.GetByProposalIdAsync(It.IsAny<string>(), ProposalId3))
            .ReturnsAsync(new List<RankingAppIndex>
            {
                rankingAppIndex
            });
        mock.Setup(o => o.GetNeedMoveRankingAppListAsync()).ReturnsAsync(new List<RankingAppIndex>()
        {
            rankingAppIndex
        });
        mock.Setup(o => o.GetByProposalIdAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(
            new List<RankingAppIndex>()
            {
                rankingAppIndex
            });
        return mock.Object;
    }

    private IRankingAppPointsRedisProvider MockRankingAppPointsRedisProvider()
    {
        var mock = new Mock<IRankingAppPointsRedisProvider>();
        var rankingAppPointsDto = new RankingAppPointsDto
        {
            ProposalId = ProposalId1,
            Alias = "Alias",
            Points = 10,
            VotePercent = 0.1,
            PointsPercent = 0.2,
            PointsType = PointsType.Vote
        };
        mock.Setup(o => o.MultiGetAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, string>()
        {
            { Address1, "10" },
            { Address2, "20" }
        });
        
        mock.Setup(o => o.GetAllAppPointsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<RankingAppPointsDto>()
            {
                rankingAppPointsDto
            });
        return mock.Object;
    }

    private IUserBalanceProvider MockUserBalanceProvider()
    {
        var mock = new Mock<IUserBalanceProvider>();
        mock.Setup(o => o.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserBalanceIndex { Amount = 1 });
        return mock.Object;
    }

    private IProposalProvider MockProposalProvider()
    {
        var mock = new Mock<IProposalProvider>();
        mock.Setup(o => o.GetProposalByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ProposalIndex
                { ActiveStartTime = DateTime.UtcNow, ActiveEndTime = DateTime.UtcNow.AddDays(1) });
        mock.Setup(o => o.GetRankingProposalListAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<RankingType>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync(new Tuple<long, List<ProposalIndex>>(1,
                new List<ProposalIndex>
                {
                    new()
                    {
                        ProposalId = ProposalId1, ActiveStartTime = DateTime.Now,
                        ActiveEndTime = DateTime.Now.AddDays(1)
                    }
                }));
        mock.Setup(o => o.GetPollListAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<List<string>>())).ReturnsAsync(new Tuple<long, List<ProposalIndex>>(100, new List<ProposalIndex>()
        {
            new()
            {
                ProposalId = ProposalId1, ActiveStartTime = DateTime.Now,
                ActiveEndTime = DateTime.Now.AddDays(1)
            }
        }));
        mock.Setup(o => o.GetProposalByIdsAsync(It.IsAny<string>(), It.IsAny<List<string>>())).ReturnsAsync(
            new List<ProposalIndex>()
            {
                new()
                {
                    ProposalId = ProposalId1, ActiveStartTime = DateTime.Now,
                    ActiveEndTime = DateTime.Now.AddDays(1)
                }
            });
        return mock.Object;
    }

    public IOptionsMonitor<TelegramOptions> MockTelegramOptions()
    {
        var mock = new Mock<IOptionsMonitor<TelegramOptions>>();

        mock.Setup(o => o.CurrentValue).Returns(new TelegramOptions
        {
            AllowedCrawlUsers = new HashSet<string>() { Address1 },
        });

        return mock.Object;
    }

    public IVoteProvider MockVoteProvider()
    {
        var mock = new Mock<IVoteProvider>();
        mock.Setup(o => o.GetNeedMoveVoteRecordListAsync()).ReturnsAsync(new List<VoteRecordIndex>()
        {
            new VoteRecordIndex
            {
                Id = "Id",
                BlockHeight = 100,
                ChainId = ChainIdAELF,
                TransactionId = TransactionHash.ToHex(),
                DAOId = DaoId,
                VotingItemId = ProposalId1,
                Voter = Address1,
                VoteMechanism = VoteMechanism.TOKEN_BALLOT,
                Amount = 1,
                Option = VoteOption.Approved,
                VoteTime = DateTime.UtcNow.Date.AddDays(-1),
                IsWithdraw = false,
                StartTime = DateTime.UtcNow.Date.AddDays(-1),
                EndTime = DateTime.UtcNow.Date.AddDays(1),
                Memo = null,
                ValidRankingVote = false,
                Alias = "Alias",
                Title = "Title",
                TotalRecorded = false
            }
        });
        mock.Setup(o =>
                o.GetByVoterAndVotingItemIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<VoteRecordIndex>()
            {
                new VoteRecordIndex
                {
                    Id = "Id",
                    BlockHeight = 100,
                    ChainId = ChainIdAELF,
                    TransactionId = TransactionHash.ToHex(),
                    DAOId = DaoId,
                    VotingItemId = ProposalId1,
                    Voter = Address1,
                    VoteMechanism = VoteMechanism.TOKEN_BALLOT,
                    Amount = 1,
                    Option = VoteOption.Approved,
                    VoteTime = DateTime.UtcNow.Date.AddDays(-1),
                    IsWithdraw = false,
                    StartTime = DateTime.UtcNow.Date.AddDays(-1),
                    EndTime = DateTime.UtcNow.Date.AddDays(1),
                    Memo = null,
                    ValidRankingVote = false,
                    Alias = "Alias",
                    Title = "Title",
                    TotalRecorded = false
                }
            });
        mock.Setup(o => o.GetDistinctVotersAsync(It.IsAny<string>())).ReturnsAsync(new List<string>()
        {
            Address1, Address2
        });
        return mock.Object;
    }
}