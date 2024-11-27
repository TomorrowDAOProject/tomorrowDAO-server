using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Enums;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Ranking.Provider;

public class RankingAppPointsCalcProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IRankingAppPointsCalcProvider _pointsCalcProvider;
    private readonly RankingAppPointsCalcProvider _pointsCalcProviderImpl;

    public RankingAppPointsCalcProviderTest(ITestOutputHelper output) : base(output)
    {
        _pointsCalcProvider = Application.ServiceProvider.GetRequiredService<IRankingAppPointsCalcProvider>();
        _pointsCalcProviderImpl = Application.ServiceProvider.GetRequiredService<RankingAppPointsCalcProvider>();
    }

    [Fact]
    public async Task OptionsTest()
    {
        var option = _pointsCalcProvider.CalculatePointsFromDailyViewAsset();
        option.ShouldBe(1_0000);

        option = _pointsCalcProvider.CalculatePointsFromDailyFirstInvite();
        option.ShouldBe(2_0000);

        option = _pointsCalcProvider.CalculatePointsFromExploreJoinTgChannel();
        option.ShouldBe(1_0000);

        option = _pointsCalcProvider.CalculatePointsFromExploreFollowX();
        option.ShouldBe(1_0000);

        option = _pointsCalcProvider.CalculatePointsFromExploreJoinDiscord();
        option.ShouldBe(1_0000);

        option = _pointsCalcProvider.CalculatePointsFromExploreCumulateFiveInvite();
        option.ShouldBe(10_0000);

        option = _pointsCalcProvider.CalculatePointsFromExploreCumulateTenInvite();
        option.ShouldBe(30_0000);

        option = _pointsCalcProvider.CalculatePointsFromExploreCumulateTwentyInvite();
        option.ShouldBe(50_0000);

        option = _pointsCalcProvider.CalculatePointsFromPointsExploreForwardX();
        option.ShouldBe(1_0000);
    }

    [Fact]
    public async Task CalculatePointsFromPointsTypeTest()
    {
        var count = 1;
        var names = (PointsType[])Enum.GetValues(typeof(PointsType));
        foreach (var name in names)
        {
            var points = _pointsCalcProvider.CalculatePointsFromPointsType(name, 1);
            var point1 = 0L;
            switch (name)
            {
                case PointsType.Vote:
                    point1 = _pointsCalcProvider.CalculatePointsFromVotes(count);
                    break;
                case PointsType.Like:
                    point1 = _pointsCalcProvider.CalculatePointsFromLikes(count);
                    break;
                case PointsType.InviteVote:
                case PointsType.BeInviteVote:
                    point1 = _pointsCalcProvider.CalculatePointsFromReferralVotes(count);
                    break;
                case PointsType.TopInviter:
                    point1 = _pointsCalcProvider.CalculatePointsFromReferralTopInviter();
                    break;
                case PointsType.DailyViewAsset:
                    point1 = _pointsCalcProvider.CalculatePointsFromDailyViewAsset();
                    break;
                case PointsType.DailyFirstInvite:
                    point1 = _pointsCalcProvider.CalculatePointsFromDailyFirstInvite();
                    break;
                case PointsType.ExploreJoinTgChannel:
                    point1 = _pointsCalcProvider.CalculatePointsFromExploreJoinTgChannel();
                    break;
                case PointsType.ExploreFollowX:
                    point1 = _pointsCalcProvider.CalculatePointsFromExploreFollowX();
                    break;
                case PointsType.ExploreJoinDiscord:
                    point1 = _pointsCalcProvider.CalculatePointsFromExploreJoinDiscord();
                    break;
                case PointsType.ExploreCumulateFiveInvite:
                    point1 = _pointsCalcProvider.CalculatePointsFromExploreCumulateFiveInvite();
                    break;
                case PointsType.ExploreCumulateTenInvite:
                    point1 = _pointsCalcProvider.CalculatePointsFromExploreCumulateTenInvite();
                    break;
                case PointsType.ExploreCumulateTwentyInvite:
                    point1 = _pointsCalcProvider.CalculatePointsFromExploreCumulateTwentyInvite();
                    break;
                case PointsType.ExploreForwardX:
                    point1 = _pointsCalcProvider.CalculatePointsFromPointsExploreForwardX();
                    break;
                case PointsType.DailyViewAds:
                    point1 = _pointsCalcProvider.CalculatePointsFromDailyViewAds();
                    break;
            }
            point1.ShouldBe(points);
        }
    }
}