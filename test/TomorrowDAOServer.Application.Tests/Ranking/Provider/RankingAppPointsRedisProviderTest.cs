using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Ranking.Dto;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Ranking.Provider;

public partial class RankingAppPointsRedisProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;

    public RankingAppPointsRedisProviderTest(ITestOutputHelper output) : base(output)
    {
        _rankingAppPointsRedisProvider =
            Application.ServiceProvider.GetRequiredService<IRankingAppPointsRedisProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockConnectionMultiplexer());
        services.AddSingleton(MockDistributedCache());
    }

    [Fact]
    public async Task SetAsyncTest()
    {
        await _rankingAppPointsRedisProvider.SetAsync("key", "10", TimeSpan.MaxValue);
    }

    [Fact]
    public async Task GetAllAppPointsAsyncTest()
    {
        var appPoints = await _rankingAppPointsRedisProvider.GetAllAppPointsAsync(ChainIdAELF, ProposalId1,
            new List<string>() { "Alias", "Bcc" });
        appPoints.ShouldNotBeNull();
        appPoints.Count.ShouldBe(1);
        appPoints[0].Points.ShouldBe(1);
    }

    [Fact]
    public async Task GetDefaultAllAppPointsAsync()
    {
        var defaultAllApp = await _rankingAppPointsRedisProvider.GetDefaultAllAppPointsAsync(ChainIdAELF);
        defaultAllApp.ShouldNotBeNull();
        defaultAllApp.Count.ShouldBe(1);
        defaultAllApp[0].Points.ShouldBe(1);
    }

    [Fact]
    public async Task GetUserAllPointsAsyncTest()
    {
        var points = await _rankingAppPointsRedisProvider.GetUserAllPointsByAddressAsync(Address1);
        points.ShouldBe(2);
    }

    [Fact]
    public async Task IncrementLikePointsAsyncTest()
    {
        var (aliasLikeCountDic, addedAliasDic) = await _rankingAppPointsRedisProvider.IncrementLikePointsAsync(new RankingAppLikeInput
        {
            ChainId = ChainIdAELF,
            ProposalId = "ProposalId",
            LikeList = new List<RankingAppLikeDetailDto>()
            {
                new RankingAppLikeDetailDto
                {
                    Alias = "Alias1",
                    LikeAmount = 10
                },
                new RankingAppLikeDetailDto
                {
                    Alias = "Alias2",
                    LikeAmount = 20
                }
            }
        }, "address");
        aliasLikeCountDic.ShouldNotBeNull();
        addedAliasDic.ShouldNotBeNull();
    }

    [Fact]
    public async Task IncrementVotePointsAsyncTest()
    {
        var result = await _rankingAppPointsRedisProvider.IncrementVotePointsAsync(ChainIdAELF, "proposalId", "addressvote",
            "aliasvote", 10);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IncrementReferralVotePointsAsyncTest()
    {
        await _rankingAppPointsRedisProvider.IncrementReferralVotePointsAsync("inviters", "invitees", 1);
    }

    [Fact]
    public async Task IncrementReferralTopInviterPointsAsyncTest()
    {
        await _rankingAppPointsRedisProvider.IncrementReferralTopInviterPointsAsync("addressreferral");
    }

    [Fact]
    public async Task IncrementViewAdPointsAsyncTest()
    {
        await _rankingAppPointsRedisProvider.IncrementViewAdPointsAsync("addressviewad");
    }

    [Fact]
    public async Task IncrementLoginPointsByUserIdAsyncTest()
    {
        await _rankingAppPointsRedisProvider.IncrementLoginPointsByUserIdAsync("userIdloginpoints", true, 3);
    }

    [Fact]
    public async Task GetAppLikeCountAsyncTest()
    {
        var dictionary = await _rankingAppPointsRedisProvider.GetAppLikeCountAsync(new List<string>()
        {
            "aliases"
        });
        dictionary.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetTotalVotesAsyncTest()
    {
        var totalVotes = await _rankingAppPointsRedisProvider.GetTotalVotesAsync();
        totalVotes.ShouldBe(2);
    }

    [Fact]
    public async Task GetTotalLikesAsyncTest()
    {
        var totalLikes = await _rankingAppPointsRedisProvider.GetTotalLikesAsync();
        totalLikes.ShouldBe(2);
    }
}