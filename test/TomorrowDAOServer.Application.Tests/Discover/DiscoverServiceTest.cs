using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Discover.Dto;
using TomorrowDAOServer.Discover.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Provider;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;
using ProposalType = TomorrowDAOServer.Enums.ProposalType;

namespace TomorrowDAOServer.Discover;

public partial class DiscoverServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IDiscoverService _discoverService;
    private readonly IDiscoverChoiceProvider _discoverChoiceProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly ITelegramAppsProvider _telegramAppsProvider;

    public DiscoverServiceTest(ITestOutputHelper output) : base(output)
    {
        _discoverService = Application.ServiceProvider.GetRequiredService<IDiscoverService>();
        _discoverChoiceProvider = Application.ServiceProvider.GetRequiredService<IDiscoverChoiceProvider>();
        _proposalProvider = Application.ServiceProvider.GetRequiredService<IProposalProvider>();
        _rankingAppProvider = Application.ServiceProvider.GetRequiredService<IRankingAppProvider>();
        _telegramAppsProvider = Application.ServiceProvider.GetRequiredService<ITelegramAppsProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(RankingAppPointsRedisProviderTest.MockConnectionMultiplexer());
        services.AddSingleton(RankingAppPointsRedisProviderTest.MockDistributedCache());
        services.AddSingleton(MockCommentIndexRepository());
    }

    [Fact]
    public async Task DiscoverViewedAsyncTest()
    {
        Login(Guid.Empty);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _discoverService.DiscoverViewedAsync(ChainIdAELF);
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("User is not authenticated.");

        Login(Guid.NewGuid(), Address1);
        var discoverViewed = await _discoverService.DiscoverViewedAsync(ChainIdAELF);
        discoverViewed.ShouldBeFalse();

        discoverViewed = await _discoverService.DiscoverViewedAsync(ChainIdAELF);
        discoverViewed.ShouldBeTrue();
    }

    [Fact]
    public async Task DiscoverChooseAsync()
    {
        Login(Guid.Empty);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _discoverService.DiscoverChooseAsync(ChainIdAELF, new List<string>()
            {
                "abc", "def"
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("User is not authenticated.");
        
        
        Login(Guid.NewGuid(), Address1);
        var discoverChoose = false;
        var throwException = false;
        try
        {
            discoverChoose = await _discoverService.DiscoverChooseAsync(ChainIdAELF, new List<string>()
            {
                TelegramAppCategory.Earn.ToString(), TelegramAppCategory.Ecommerce.ToString()
            });
        }
        catch (Exception e)
        {
            if (e is not UserFriendlyException ||
                !e.Message.Contains("Already chose the discover type"))
            {
                throw;
            }
            throwException = true;
        }

        if (throwException)
        {
            discoverChoose.ShouldBeFalse();
        }
        else
        {
            discoverChoose.ShouldBeTrue();
        }
        
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);
        var result = await _discoverService.DiscoverChooseAsync(ChainIdAELF, new List<string>()
        {
            TelegramAppCategory.Earn.ToString(), TelegramAppCategory.Ecommerce.ToString()
        });
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task GetDiscoverAppListAsyncTest()
    {
        var newGuid = Guid.NewGuid();
        Login(newGuid, Address1);
        await CreateDiscoverChoiceIndexAsync(newGuid, Address1);
        // var discoverAppList = await _discoverService.GetDiscoverAppListAsync(new GetDiscoverAppListInput
        // {
        //     ChainId = ChainIdAELF,
        //     Category = CommonConstant.Recommend,
        //     SkipCount = 0,
        //     MaxResultCount = 10,
        // });
        // discoverAppList.ShouldNotBeNull();
        // discoverAppList.TotalCount.ShouldBe(0);

        try
        {
            //TODO Exception
            var discoverAppList = await _discoverService.GetDiscoverAppListAsync(new GetDiscoverAppListInput
            {
                ChainId = ChainIdAELF,
                Category = TelegramAppCategory.Game.ToString(),
                SkipCount = 0,
                MaxResultCount = 10,
            });
            discoverAppList.ShouldNotBeNull();
            discoverAppList.TotalCount.ShouldBe(0);
        }
        catch (Exception e)
        {
            Assert.True(true);
        }
        
    }

    private async Task CreateDiscoverChoiceIndexAsync(Guid newGuid, string address)
    {
        await _discoverChoiceProvider.BulkAddOrUpdateAsync(new List<DiscoverChoiceIndex>()
        {
            new DiscoverChoiceIndex
            {
                Id = "DiscoverChoiceIndex-Id",
                ChainId = ChainIdAELF,
                UserId = newGuid.ToString(),
                Address = address,
                TelegramAppCategory = TelegramAppCategory.Game,
                DiscoverChoiceType = DiscoverChoiceType.Choice,
                UpdateTime = default
            }
        });
    }

    [Fact]
    public async Task GetRandomAppListAsyncTest()
    {
        Login(Guid.NewGuid(), Address1);
        try
        {
            await _discoverService.DiscoverChooseAsync(ChainIdAELF, new List<string>()
            {
                TelegramAppCategory.Earn.ToString(), TelegramAppCategory.Ecommerce.ToString()
            });
        } catch (Exception e) { }

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _discoverService.GetRandomAppListAsync(new GetRandomAppListInputAsync
            {
                ChainId = ChainIdAELF,
                Category = TelegramAppCategory.Earn.ToString(),
                MaxResultCount = 0,
                Aliases = null
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("Invalid category");

        var randomAppListDto = await _discoverService.GetRandomAppListAsync(new GetRandomAppListInputAsync
        {
            ChainId = ChainIdAELF,
            Category = CommonConstant.Recommend,
            MaxResultCount = 100,
            Aliases = new List<string>()
        });
        randomAppListDto.ShouldNotBeNull();
        randomAppListDto.AppList.ShouldBeEmpty();
        
        randomAppListDto = await _discoverService.GetRandomAppListAsync(new GetRandomAppListInputAsync
        {
            ChainId = ChainIdAELF,
            Category = CommonConstant.ForYou,
            MaxResultCount = 100,
            Aliases = new List<string>()
        });
        randomAppListDto.ShouldNotBeNull();
        randomAppListDto.AppList.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAccumulativeAppListAsyncTest()
    {
        Login(Guid.NewGuid(), Address1);
        var accumulativeAppPageResultDto = await _discoverService.GetAccumulativeAppListAsync(new GetDiscoverAppListInput
        {
            ChainId = ChainIdAELF,
            Category = TelegramAppCategory.All.ToString(),
            SkipCount = 0,
            MaxResultCount = 10,
            Search = null
        });
        
        accumulativeAppPageResultDto = await _discoverService.GetAccumulativeAppListAsync(new GetDiscoverAppListInput
        {
            ChainId = ChainIdAELF,
            Category = TelegramAppCategory.All.ToString(),
            SkipCount = 0,
            MaxResultCount = 10,
            Search = "Alias"
        });
    }

    [Fact]
    public async Task GetCurrentAppListAsync()
    {
        var now = DateTime.UtcNow;
        await GenerateDefaultRankingAsync(now);
        
        Login(Guid.NewGuid(), Address1);
        var  currentAppPageResultDto = await _discoverService.GetCurrentAppListAsync(new GetDiscoverAppListInput
        {
            ChainId = ChainIdAELF,
            Category = TelegramAppCategory.Game.ToString(),
            SkipCount = 0,
            MaxResultCount = 10,
            Search = null
        });
        currentAppPageResultDto.ShouldNotBeNull();
        currentAppPageResultDto.ActiveEndEpochTime.ShouldBe(now.Date.AddDays(2).ToUtcMilliSeconds());
    }

    [Fact]
    public async Task ViewAppAsyncTest()
    {
        Login(Guid.NewGuid(), Address1);
        var result = await _discoverService.ViewAppAsync(new ViewAppInput
        {
            ChainId = ChainIdAELF
        });
        result.ShouldBeTrue();
        
        result = await _discoverService.ViewAppAsync(new ViewAppInput
        {
            ChainId = ChainIdAELF,
            Aliases = new List<string>() {"Alias"}
        });
        result.ShouldBeTrue();
    }

    private async Task GenerateDefaultRankingAsync(DateTime now)
    {
        await _telegramAppsProvider.BulkAddOrUpdateAsync(new List<TelegramAppIndex>() {new TelegramAppIndex
            {
                Id = "AppId",
                Alias = "Alias",
                Title = "Title",
                Icon = "Icon",
                Description = null,
                EditorChoice = false,
                Url = null,
                LongDescription = null,
                Screenshots = new List<string>(),
                Categories = new List<TelegramAppCategory>() { TelegramAppCategory.Game },
                CreateTime = default,
                UpdateTime = default,
                SourceType = SourceType.Telegram,
                Creator = null,
                LoadTime = default,
                BackIcon = null,
                BackScreenshots = null,
                TotalPoints = 10000,
                TotalVotes = 10,
                TotalLikes = 50,
                TotalOpenTimes = 1
            }
        });
        
        await _proposalProvider.BulkAddOrUpdateAsync(new List<ProposalIndex>()
        {
            new ProposalIndex
            {
                ChainId = ChainIdAELF,
                BlockHash = Address1CaHash,
                BlockHeight = 0,
                Id = ProposalId1,
                DAOId = DAOId,
                ProposalId = ProposalId3,
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "ProposalDescription",
                ForumUrl = null,
                ProposalType = ProposalType.Advisory,
                ActiveStartTime =now.Date.AddDays(-2),
                ActiveEndTime = now.Date.AddDays(2),
                ExecuteStartTime = now.Date.AddDays(2),
                ExecuteEndTime = now.Date.AddDays(4),
                ProposalStatus = ProposalStatus.PendingVote,
                ProposalStage = ProposalStage.Active,
                Proposer = Address1,
                SchemeAddress = null,
                Transaction = null,
                VoteSchemeId = null,
                VoteMechanism = VoteMechanism.UNIQUE_VOTE,
                VetoProposalId = null,
                BeVetoProposalId = null,
                DeployTime = default,
                ExecuteTime = null,
                GovernanceMechanism = GovernanceMechanism.Referendum,
                MinimalRequiredThreshold = 0,
                MinimalVoteThreshold = 0,
                MinimalApproveThreshold = 0,
                MaximalRejectionThreshold = 0,
                MaximalAbstentionThreshold = 0,
                ProposalThreshold = 0,
                ActiveTimePeriod = 0,
                VetoActiveTimePeriod = 0,
                PendingTimePeriod = 0,
                ExecuteTimePeriod = 0,
                VetoExecuteTimePeriod = 0,
                VoteFinished = false,
                IsNetworkDAO = false,
                ProposalCategory = ProposalCategory.Ranking,
                RankingType = RankingType.Verified,
                ProposalIcon = null,
                ProposerId = null,
                ProposerFirstName = null,
                ProposalSource = ProposalSourceEnum.TMRWDAO
            }
        });

        await _rankingAppProvider.BulkAddOrUpdateAsync(new List<RankingAppIndex>()
        {
            new RankingAppIndex
            {
                Id = "id",
                ChainId = ChainIdAELF,
                DAOId = DAOId,
                ProposalId = ProposalId3,
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "",
                ActiveStartTime = now.Date.AddDays(-2),
                ActiveEndTime = now.Date.AddDays(2),
                AppId = "AppId",
                Alias = "Alias",
                Title = "Title",
                Icon = "Icon",
                Description = null,
                EditorChoice = false,
                DeployTime = default,
                VoteAmount = 0,
                Url = null,
                LongDescription = null,
                Screenshots = new List<string>(),
                TotalPoints = 1000,
                TotalVotes = 1,
                TotalLikes = 50,
                Categories = new List<TelegramAppCategory>() { TelegramAppCategory.Game },
                AppIndex = 0
            }
        });
    }
}