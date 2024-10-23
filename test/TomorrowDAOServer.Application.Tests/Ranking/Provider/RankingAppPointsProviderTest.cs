using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Ranking.Eto;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Ranking.Provider;

public class RankingAppPointsProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IRankingAppPointsProvider _rankingAppPointsProvider;
    
    public RankingAppPointsProviderTest(ITestOutputHelper output) : base(output)
    {
        _rankingAppPointsProvider = Application.ServiceProvider.GetRequiredService<IRankingAppPointsProvider>();
    }

    [Fact]
    public async Task AddOrUpdateAppPointsIndexAsyncTest()
    {
        await _rankingAppPointsProvider.AddOrUpdateAppPointsIndexAsync(new VoteAndLikeMessageEto
        {
            PointsType = PointsType.All
        });

        await _rankingAppPointsProvider.AddOrUpdateAppPointsIndexAsync(new VoteAndLikeMessageEto
        {
            ChainId = ChainIdAELF,
            DaoId = DaoId,
            ProposalId = ProposalId1,
            AppId = "AppId",
            Alias = "Alias",
            Title = "Tile",
            Address = Address1,
            Amount = 10,
            PointsType = PointsType.Vote
        });
        
        await _rankingAppPointsProvider.AddOrUpdateAppPointsIndexAsync(new VoteAndLikeMessageEto
        {
            ChainId = ChainIdAELF,
            DaoId = DaoId,
            ProposalId = ProposalId1,
            AppId = "AppId",
            Alias = "Alias",
            Title = "Tile",
            Address = Address1,
            Amount = 10,
            PointsType = PointsType.Like
        });
    }

    [Fact]
    public async Task AddOrUpdateUserPointsIndexAsyncTest()
    {
        await _rankingAppPointsProvider.AddOrUpdateUserPointsIndexAsync(new VoteAndLikeMessageEto
        {
            PointsType = PointsType.All
        });
        
        await _rankingAppPointsProvider.AddOrUpdateUserPointsIndexAsync(new VoteAndLikeMessageEto
        {
            ChainId = ChainIdAELF,
            DaoId = DaoId,
            ProposalId = ProposalId1,
            AppId = "AppId",
            Alias = "Alias",
            Title = "Tile",
            Address = Address1,
            Amount = 10,
            PointsType = PointsType.Vote
        });
        
        await _rankingAppPointsProvider.AddOrUpdateUserPointsIndexAsync(new VoteAndLikeMessageEto
        {
            ChainId = ChainIdAELF,
            DaoId = DaoId,
            ProposalId = ProposalId1,
            AppId = "AppId",
            Alias = "Alias",
            Title = "Tile",
            Address = Address1,
            Amount = 10,
            PointsType = PointsType.Like
        });
    }

    [Fact]
    public async Task GetRankingAppPointsIndexByAliasAsyncTest()
    {
        var pointsIndex = await _rankingAppPointsProvider.GetRankingAppPointsIndexByAliasAsync(ChainIdAELF, ProposalId1);
        pointsIndex.ShouldNotBeNull();
        pointsIndex.Points.ShouldBeGreaterThan(10);
    }

    [Fact]
    public async Task GetRankingUserPointsIndexByAliasAsyncTest()
    {
        var pointsIndex = await _rankingAppPointsProvider.GetRankingUserPointsIndexByAliasAsync(ChainIdAELF, ProposalId1, Address1);
        pointsIndex.ShouldNotBeNull();
        pointsIndex.Points.ShouldBeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public async Task GetTotalPointsByAliasAsyncTest()
    {
        var points = await _rankingAppPointsProvider.GetTotalPointsByAliasAsync(ChainIdAELF, new List<string>() { "Alias", "Tests" });
        points.ShouldNotBeNull();
        points.Keys.ShouldContain("Alias");
    }
    
}