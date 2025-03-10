using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aetherlink.PriceServer.Common;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;
using ProposalType = TomorrowDAOServer.Enums.ProposalType;

namespace TomorrowDAOServer.User;

public partial class UserServiceTest : TomorrowDaoServerApplicationTestBase
{
    private IUserService _userService;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IUserAppService _userAppService;
    
    public UserServiceTest(ITestOutputHelper output) : base(output)
    {
        _userService = Application.ServiceProvider.GetRequiredService<IUserService>();
        _telegramAppsProvider = Application.ServiceProvider.GetRequiredService<ITelegramAppsProvider>();
        _userAppService = Application.ServiceProvider.GetRequiredService<IUserAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockConnectionMultiplexer());
        services.AddSingleton(MockDistributedCache());
    }

    [Fact]
    public async Task Test()
    {
        Assert.True(true);
    }

    [Fact]
    public async Task UserSourceReportAsyncTest()
    {
        Login(Guid.NewGuid(), Address1);

        var userSourceReportResultDto = await _userService.UserSourceReportAsync(ChainIdAELF, "source");
        userSourceReportResultDto.ShouldNotBeNull();
        userSourceReportResultDto.Success.ShouldBeFalse();
        userSourceReportResultDto.Reason.ShouldBe("Invalid source.");
        
        userSourceReportResultDto = await _userService.UserSourceReportAsync(ChainIdAELF, "UserSource1");
        userSourceReportResultDto.ShouldNotBeNull();
        userSourceReportResultDto.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task CompleteTaskAsyncTest()
    {
        Login(Guid.NewGuid(), Address1);
        var result = await _userService.CompleteTaskAsync(new CompleteTaskInput
        {
            ChainId = ChainIdAELF,
            UserTask = UserTask.ExploreVotigram.ToString(),
            UserTaskDetail = UserTaskDetail.ExploreJoinVotigram.ToString()
        });
        result.ShouldBeTrue();
        
        // result = await _userService.CompleteTaskAsync(new CompleteTaskInput
        // {
        //     ChainId = ChainIdAELF,
        //     UserTask = UserTask.ExploreApps.ToString(),
        //     UserTaskDetail = UserTaskDetail.ExploreSchrodinger.ToString()
        // });
        // result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetMyPointsAsyncTest()
    {
       var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);
        await CompleteTaskAsyncTest();
        var voteHistoryPagedResultDto = await _userService.GetMyPointsAsync(new GetMyPointsInput
        {
            ChainId = ChainIdAELF,
            SkipCount = 0,
            MaxResultCount = 10
        });
        voteHistoryPagedResultDto.ShouldNotBeNull();
        voteHistoryPagedResultDto.Items.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetTaskListAsyncTest()
    {
        Login(Guid.NewGuid(), Address1);
        var taskListDto = await _userService.GetTaskListAsync(ChainIdAELF);
        taskListDto.ShouldNotBeNull();
        taskListDto.TaskList.ShouldNotBeEmpty();
        taskListDto.TaskList.Count.ShouldBe(4);
    }

    [Fact]
    public async Task ViewAdAsyncTest()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);


        var timeStamp = DateTime.UtcNow.Date.Millisecond;
        var sha256Hash = Sha256HashHelper.ComputeSha256Hash(IdGeneratorHelper.GenerateId("CheckKey", timeStamp));

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            var result = await _userService.ViewAdAsync(new ViewAdInput
            {
                ChainId = ChainIdAELF,
                Signature = sha256Hash,
                TimeStamp = timeStamp
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("Invalid timeStamp");
    }

    [Fact]
    public async Task SaveTgInfoAsyncTest()
    {
        var result = await _userService.SaveTgInfoAsync(new SaveTgInfoInput
        {
            ChainId = ChainIdAELF,
            FirstName = "FirstName",
            LastName = "LastName",
            UserName = "UserName",
            Icon = "Icon",
            TelegramId = "TelegramId"
        });
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task GenerateDailyCreatePollPointsAsyncTest()
    {
        await _userService.GenerateDailyCreatePollPointsAsync(ChainIdAELF, new List<IndexerProposal>()
        {
            new IndexerProposal
            {
                 ChainId = ChainIdAELF,
                BlockHeight = 0,
                Id = "Id",
                DAOId = DAOId,
                ProposalId = "ProposalId-ProposalId",
                ProposalTitle = "ProposalTitle",
                ProposalDescription = "ProposalDescription",
                ForumUrl = null,
                ProposalType = ProposalType.Advisory,
                ActiveStartTime =DateTime.UtcNow.Date.AddDays(-2),
                ActiveEndTime = DateTime.UtcNow.Date.AddDays(2),
                ExecuteStartTime = DateTime.UtcNow.Date.AddDays(2),
                ExecuteEndTime = DateTime.UtcNow.Date.AddDays(4),
                ProposalStatus = ProposalStatus.PendingVote,
                ProposalStage = ProposalStage.Active,
                Proposer = Address1,
                SchemeAddress = null,
                Transaction = null,
                VoteSchemeId = null,
                VetoProposalId = null,
                BeVetoProposalId = null,
                DeployTime = DateTime.UtcNow,
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
            }
        });
    }

    [Fact]
    public async Task GetLoginPointsStatusAsyncTest()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);
        var loginPointsStatusDto = await _userService.GetLoginPointsStatusAsync(new GetLoginPointsStatusInput
        {
            ChainId = ChainIdAELF
        });
        loginPointsStatusDto.ShouldNotBeNull();
        loginPointsStatusDto.ConsecutiveLoginDays.ShouldBe(1);
    }

    [Fact]
    public async Task CollectLoginPointsAsyncTest()
    {
        await GetLoginPointsStatusAsyncTest();
        var timeStamp = DateTime.UtcNow.Date.ToUtcMilliSeconds();
        var sha256Hash = Sha256HashHelper.ComputeSha256Hash(IdGeneratorHelper.GenerateId("CheckKey", timeStamp));
        var loginPointsStatusDto = await _userService.CollectLoginPointsAsync(new CollectLoginPointsInput
        {
            ChainId = ChainIdAELF,
            Signature = sha256Hash,
            TimeStamp = timeStamp.ToString()
        });
        loginPointsStatusDto.ShouldNotBeNull();
        loginPointsStatusDto.ConsecutiveLoginDays.ShouldBe(1);
    }

    [Fact]
    public async Task GetHomePageAsyncTest()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);
        var homePageResultDto = await _userService.GetHomePageAsync(new GetHomePageInput
        {
            ChainId = ChainIdAELF
        });
        homePageResultDto.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetMadeForYouAsyncTest()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);
        var pageResultDto = await _userService.GetMadeForYouAsync(new GetMadeForYouInput
        {
            ChainId = ChainIdAELF,
            SkipCount = 0,
            MaxResultCount = 10
        });
        pageResultDto.ShouldNotBeNull();
    }

    [Fact]
    public async Task OpenAppAsyncTest()
    {
        await GenerateTelegramAppIndexAsync();
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);
        var result = await _userService.OpenAppAsync(new OpenAppInput
        {
            ChainId = ChainIdAELF,
            Alias = null
        });
        result.ShouldBe(false);
        
        result = await _userService.OpenAppAsync(new OpenAppInput
        {
            ChainId = ChainIdAELF,
            Alias = "Alias"
        });
        result.ShouldBe(true);
    }

    [Fact]
    public async Task ShareAppAsyncTest()
    {
        await GenerateTelegramAppIndexAsync();
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);

        var result = await _userService.ShareAppAsync(new ShareAppInput());
        result.ShouldBe(false);

        result = await _userService.ShareAppAsync(new ShareAppInput
        {
            ChainId = ChainIdAELF,
            Alias = "NotExistAlias"
        });
        result.ShouldBeTrue();
        
        result = await _userService.ShareAppAsync(new ShareAppInput
        {
            ChainId = ChainIdAELF,
            Alias = "Alias"
        });
        result.ShouldBe(true);
    }

    [Fact]
    public async Task CheckPointsAsyncTest()
    {
        var telegramUserId = "33333333333";
        var result = await _userService.CheckPointsAsync(telegramUserId);
        result.ShouldBeFalse();
        
        var address = Base58Encoder.GenerateRandomBase58String(50);
        var userIdA = Guid.NewGuid();
        await CreateUserIndexAsync(userIdA, address, telegramUserId);
        result = await _userService.CheckPointsAsync(telegramUserId);
        result.ShouldBeTrue();
        
        var userIdB = Guid.NewGuid();
        await CreateUserIndexAsync(userIdB, address, telegramUserId);
        result = await _userService.CheckPointsAsync(telegramUserId);
        result.ShouldBeTrue();
        
        await CreateUserIndexAsync(userIdA, address, Guid.NewGuid().ToString());
        await CreateUserIndexAsync(userIdB, address, Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task GetAllUserPointsAsyncTest()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _userService.GetAllUserPointsAsync(new GetAllUserPointsInput
            {
                ChainId = ChainIdAELF
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("Access denied.");
        
        
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);
        await CreateUserIndexAsync(Guid.NewGuid(), address, Guid.NewGuid().ToString());
        var resultDto = await _userService.GetAllUserPointsAsync(new GetAllUserPointsInput
        {
            ChainId = ChainIdAELF
        });
        resultDto.ShouldNotBeNull();
        resultDto.TotalCount.ShouldBeGreaterThan(0);
        resultDto.Items.ShouldNotBeEmpty();
    }

    private async Task GenerateTelegramAppIndexAsync()
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
    }

    private async Task CreateUserIndexAsync(Guid userId, string address, string telegramUserId)
    {
        await _userAppService.CreateUserAsync(new UserDto
        {
            Id = userId,
            AppId = "AppId",
            UserId = userId,
            UserName = "UserName",
            CaHash = "CaHash",
            AddressInfos = new List<AddressInfo>()
            {
                new AddressInfo
                {
                    ChainId = ChainIdAELF,
                    Address = address
                }
            },
            CreateTime = DateTime.Now.ToUtcSeconds(),
            ModificationTime = DateTime.Now.ToUtcSeconds(),
            Address = address,
            Extra = JsonConvert.SerializeObject(new UserExtraDto
            {
                ConsecutiveLoginDays = 0,
                LastModifiedTime = default,
                DailyPointsClaimedStatus = new bool[]
                {
                },
                HasVisitedVotePage = false
            }),
            UserInfo = JsonConvert.SerializeObject(new TelegramAuthDataDto
            {
                Id = telegramUserId,
                UserName = "UserName",
                AuthDate = "AuthDate",
                FirstName = "FirstName",
                LastName = "LastName",
                Hash = "Hash",
                PhotoUrl = "PhotoUrl",
                BotId = "BotId"
            })
        });
    }
}