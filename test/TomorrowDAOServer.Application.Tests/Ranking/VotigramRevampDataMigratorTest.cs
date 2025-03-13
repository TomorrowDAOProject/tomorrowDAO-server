using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Dtos;
using TomorrowDAOServer.User.Provider;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Ranking;

public class VotigramRevampDataMigratorTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IVotigramRevampDataMigrator _votigramRevampDataMigrator;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly IRankingAppPointsProvider _rankingAppPointsProvider;
    private readonly IUserPointsRecordProvider _userPointsRecordProvider;
    private readonly IUserAppService _userAppService;

    public VotigramRevampDataMigratorTest(ITestOutputHelper output) : base(output)
    {
        _votigramRevampDataMigrator = Application.ServiceProvider.GetRequiredService<IVotigramRevampDataMigrator>();
        _telegramAppsProvider = Application.ServiceProvider.GetRequiredService<ITelegramAppsProvider>();
        _rankingAppProvider = Application.ServiceProvider.GetRequiredService<IRankingAppProvider>();
        _rankingAppPointsProvider = Application.ServiceProvider.GetRequiredService<IRankingAppPointsProvider>();
        _userPointsRecordProvider = Application.ServiceProvider.GetRequiredService<IUserPointsRecordProvider>();
        _userAppService = Application.ServiceProvider.GetRequiredService<IUserAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(RankingAppPointsRedisProviderTest.MockConnectionMultiplexer());
        services.AddSingleton(RankingAppPointsRedisProviderTest.MockDistributedCache());
    }

    [Fact]
    public async Task MigrateHistoricalDataAsyncTest()
    {
        var newGuid = Guid.NewGuid();
        Login(newGuid, Address1);

        await GenerateTelegramAppDataAsync();
        await GenerateRankingAppAsync();
        await GenerateRankingAppPointsAsync();
        await GenerateRankingUserAppPointsAsync();
        await GenerateUserPointsRecordAsync(newGuid);
        await GenerateUserAsync();
        await _votigramRevampDataMigrator.MigrateHistoricalDataAsync(ChainIdAELF, true, true, true, true, true,
            true, true);
    }

    private async Task GenerateUserAsync()
    {
        await _userAppService.CreateUserAsync(new UserDto
        {
            Id = Guid.NewGuid(),
            AppId = ChainIdAELF,
            UserId = Guid.NewGuid(),
            UserName = Address1,
            CaHash = "CaHash",
            AddressInfos = new List<AddressInfo>()
            {
                new AddressInfo
                {
                    ChainId = ChainIdAELF,
                    Address = Address1
                }
            },
            CreateTime = DateTime.UtcNow.ToUtcMilliSeconds(),
            ModificationTime = DateTime.UtcNow.ToUtcMilliSeconds(),
            Address = Address1,
            Extra = null,
            UserInfo = null
        });
    }

    private async Task GenerateUserPointsRecordAsync(Guid newGuid)
    {
        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(ChainIdAELF, Address1,
            UserTaskDetail.ExploreJoinVotigram,
            PointsType.ExploreJoinVotigram, DateTime.UtcNow, null, newGuid.ToString());

        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(ChainIdAELF, Address1,
            UserTaskDetail.ExploreFollowX,
            PointsType.ExploreFollowX, DateTime.UtcNow, null, newGuid.ToString());

        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(ChainIdAELF, Address1,
            UserTaskDetail.ExploreForwardX,
            PointsType.ExploreForwardX, DateTime.UtcNow, null, newGuid.ToString());

        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(ChainIdAELF, Address1,
            UserTaskDetail.ExploreCumulateFiveInvite,
            PointsType.ExploreCumulateFiveInvite, DateTime.UtcNow, null, newGuid.ToString());

        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(ChainIdAELF, Address1,
            UserTaskDetail.ExploreJoinDiscord,
            PointsType.ExploreJoinDiscord, DateTime.UtcNow, null, newGuid.ToString());

        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(ChainIdAELF, Address1,
            UserTaskDetail.ExploreCumulateTwentyInvite,
            PointsType.ExploreCumulateTwentyInvite, DateTime.UtcNow, null, newGuid.ToString());

        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(ChainIdAELF, Address1,
            UserTaskDetail.ExploreJoinTgChannel,
            PointsType.ExploreJoinTgChannel, DateTime.UtcNow, null, newGuid.ToString());
    }

    private async Task GenerateRankingUserAppPointsAsync()
    {
        await _rankingAppPointsProvider.AddOrUpdateUserPointsIndexAsync(new RankingAppUserPointsIndex
        {
            Id = Guid.NewGuid(),
            ChainId = ChainIdAELF,
            DAOId = DAOId,
            ProposalId = ProposalId1,
            AppId = "AppId",
            Alias = "Alias",
            Title = "Title",
            Address = Address1,
            Amount = 10,
            Points = 100,
            UpdateTime = DateTime.UtcNow.AddDays(-1),
            PointsType = PointsType.Vote,
            UserId = "UserId"
        });

        await _rankingAppPointsProvider.AddOrUpdateUserPointsIndexAsync(new RankingAppUserPointsIndex
        {
            Id = Guid.NewGuid(),
            ChainId = ChainIdAELF,
            DAOId = DAOId,
            ProposalId = ProposalId1,
            AppId = "AppId2",
            Alias = "Alias2",
            Title = "Title2",
            Address = Address2,
            Amount = 10,
            Points = 100,
            UpdateTime = DateTime.UtcNow.AddDays(-1),
            PointsType = PointsType.InviteVote,
            UserId = "UserId2"
        });
    }

    private async Task GenerateRankingAppPointsAsync()
    {
        await _rankingAppPointsProvider.AddOrUpdateAppPointsIndexAsync(new RankingAppPointsIndex
        {
            Id = Guid.NewGuid(),
            ChainId = ChainIdAELF,
            DAOId = DAOId,
            ProposalId = ProposalId1,
            AppId = "AppId",
            Alias = "Alias",
            Title = "Title",
            Amount = 10,
            Points = 100,
            UpdateTime = DateTime.UtcNow.AddDays(-1),
            PointsType = PointsType.Vote,
            UserId = "UserId"
        });
        await _rankingAppPointsProvider.AddOrUpdateAppPointsIndexAsync(new RankingAppPointsIndex
        {
            Id = Guid.NewGuid(),
            ChainId = ChainIdAELF,
            DAOId = DAOId,
            ProposalId = ProposalId1,
            AppId = "AppId",
            Alias = "Alias",
            Title = "Title",
            Amount = 10,
            Points = 10,
            UpdateTime = DateTime.UtcNow.AddDays(-1),
            PointsType = PointsType.Like,
            UserId = "UserId"
        });
    }

    private async Task GenerateRankingAppAsync()
    {
        await _rankingAppProvider.BulkAddOrUpdateAsync(new List<RankingAppIndex>()
        {
            new RankingAppIndex
            {
                Id = "Id",
                ChainId = ChainIdAELF,
                DAOId = DAOId,
                ProposalId = ProposalId1,
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "ProposalDescription",
                ActiveStartTime = DateTime.UtcNow.Date.AddDays(-1),
                ActiveEndTime = DateTime.UtcNow.Date.AddDays(1),
                AppId = "AppId",
                Alias = "Alias",
                Title = "Title",
                Icon = "Icon",
                Description = "Description",
                EditorChoice = false,
                DeployTime = DateTime.UtcNow.Date.AddDays(-1),
                VoteAmount = 10,
                Url = string.Empty,
                LongDescription = string.Empty,
                Screenshots = new List<string>(),
                TotalPoints = 10,
                TotalVotes = 10,
                TotalLikes = 10,
                Categories = new List<TelegramAppCategory>() { TelegramAppCategory.Game },
                AppIndex = 0
            }
        });
    }

    private async Task GenerateTelegramAppDataAsync()
    {
        await _telegramAppsProvider.BulkAddOrUpdateAsync(new List<TelegramAppIndex>()
        {
            new TelegramAppIndex
            {
                Id = "id",
                Alias = "Alias",
                Title = "Title",
                Icon = "Icon",
                Description = "Description",
                EditorChoice = true,
                Url = "Url",
                LongDescription = "LongDescription",
                Screenshots = new List<string>(),
                Categories = new List<TelegramAppCategory>() { TelegramAppCategory.Game },
                CreateTime = DateTime.UtcNow.Date.AddDays(-1),
                UpdateTime = DateTime.UtcNow.Date.AddDays(-1),
                SourceType = SourceType.Telegram,
                Creator = Address1,
                LoadTime = DateTime.UtcNow.Date.AddDays(-1),
                BackIcon = null,
                BackScreenshots = null,
                TotalPoints = 10,
                TotalVotes = 10,
                TotalLikes = 10,
                TotalOpenTimes = 10
            }
        });
    }
}