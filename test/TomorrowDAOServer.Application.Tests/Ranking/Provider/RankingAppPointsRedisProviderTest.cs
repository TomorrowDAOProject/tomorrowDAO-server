using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
        var points = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(Address1);
        points.ShouldBe(2);
    }
}