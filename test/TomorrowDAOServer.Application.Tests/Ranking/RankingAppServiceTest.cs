using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Ranking.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Ranking;

public partial class RankingAppServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IRankingAppService _rankingAppService;
    
    public RankingAppServiceTest(ITestOutputHelper output) : base(output)
    {
        _rankingAppService = ServiceProvider.GetRequiredService<IRankingAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockRankingOptions());
        services.AddSingleton(MockTelegramAppsProvider());
        services.AddSingleton(MockRankingAppProvider());
        services.AddSingleton(MockDAOProvider());
        services.AddSingleton(MockRankingAppPointsRedisProvider());
        services.AddSingleton(MockAbpDistributedLock());
        services.AddSingleton(MockIDistributedCache());
        services.AddSingleton(MockUserBalanceProvider());
        services.AddSingleton(MockProposalProvider());
        services.AddSingleton(MockTelegramOptions());
        services.AddSingleton(MockVoteProvider());
    }

    [Fact]
    public async Task GenerateRankingAppTest()
    {
        await _rankingAppService.GenerateRankingApp(ChainIdAELF, new List<IndexerProposal>
        {
            new()
            {
                ProposalId = ProposalId1, ProposalDescription = "##GameRanking:crypto-bot"
            }
        });
    }

    [Fact]
    public async Task GetDefaultRankingProposalAsyncTest()
    {
        await _rankingAppService.GetDefaultRankingProposalAsync(ChainIdTDVV);
    }
    
    [Fact]
    public async Task GetRankingProposalListAsyncTest()
    {
        await _rankingAppService.GetRankingProposalListAsync(new GetRankingListInput{ChainId = ChainIdTDVV});
    }
    
    [Fact]
    public async Task GetRankingProposalDetailAsyncTest()
    {
        await _rankingAppService.GetRankingProposalDetailAsync(ChainIdTDVV, ProposalId1, DAOId);
        await _rankingAppService.GetRankingProposalDetailAsync(ChainIdTDVV, ProposalId2, DAOId);
        await _rankingAppService.GetRankingProposalDetailAsync(ChainIdTDVV, ProposalId3, DAOId);
    }
    
    [Fact]
    public async Task GetRankingProposalDetailAsyncTest2()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);

        var rankingDetailDto = await _rankingAppService.GetRankingProposalDetailAsync(ChainIdAELF, ProposalId1);
        rankingDetailDto.ShouldNotBeNull();
        rankingDetailDto.RankingList.ShouldNotBeNull();
        rankingDetailDto.RankingList.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetPollListAsyncTest()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);

        var rankingListPageResultDto = await _rankingAppService.GetPollListAsync(new GetPollListInput
        {
            MaxResultCount = 10,
            SkipCount = 0,
            ChainId = ChainIdAELF,
            Type = "Current"
        });
        rankingListPageResultDto.ShouldNotBeNull();
        rankingListPageResultDto.TotalCount.ShouldBe(100);
    }

    [Fact]
    public async Task GetVoteStatusAsyncTest()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _rankingAppService.GetVoteStatusAsync(new GetVoteStatusInput());
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("Invalid input");
        
        var rankingVoteRecord = await _rankingAppService.GetVoteStatusAsync(new GetVoteStatusInput
        {
            ChainId = ChainIdAELF,
            Address = Address1,
            ProposalId = ProposalId1,
            Category = TelegramAppCategory.Game
        });
        rankingVoteRecord.ShouldNotBeNull();
        rankingVoteRecord.TransactionId.ShouldBe(TransactionHash.ToHex());
        
        rankingVoteRecord = await _rankingAppService.GetVoteStatusAsync(new GetVoteStatusInput
        {
            ChainId = ChainIdAELF,
            Address = Address1,
            ProposalId = ProposalId1,
            Category = TelegramAppCategory.All
        });
        rankingVoteRecord.ShouldNotBeNull();
        rankingVoteRecord.TransactionId.ShouldBeNull();
    }

    [Fact]
    public async Task MoveHistoryDataAsyncTest()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), Address1);

        for (int i = 1; i < 12; i++)
        {
            await _rankingAppService.MoveHistoryDataAsync(ChainIdAELF, i.ToString(), "key", "100");
        }
    }

    [Fact]
    public async Task LikeAsyncTest()
    {
        try
        {
            await _rankingAppService.LikeAsync(new RankingAppLikeInput());
        }
        catch (Exception e)
        {
            //ExceptionHandler does not support unit testing
            Assert.True(true);
        }
        
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);
        var rankingAppLikeResultDto = await _rankingAppService.LikeAsync(new RankingAppLikeInput
        {
            ChainId = ChainIdAELF,
            ProposalId = ProposalId1,
            LikeList = new List<RankingAppLikeDetailDto>()
            {
                new RankingAppLikeDetailDto
                {
                    Alias = "Alias",
                    LikeAmount = 10
                }
            }
        });
        rankingAppLikeResultDto.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetRankingActivityResultAsyncTest()
    {
        Login(Guid.NewGuid(), Address1);
        var activityResultDto = await _rankingAppService.GetRankingActivityResultAsync(ChainIdAELF, ProposalId1, 10);
        activityResultDto.ShouldNotBeNull();
        activityResultDto.Data.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetBannerInfoAsyncTest()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), address);

        var rankingBannerInfo = await _rankingAppService.GetBannerInfoAsync(ChainIdAELF);
        rankingBannerInfo.ShouldNotBeNull();
        rankingBannerInfo.HasFire.ShouldBeFalse();
        rankingBannerInfo.NotViewedNewAppCount.ShouldBe(1);
    }
}