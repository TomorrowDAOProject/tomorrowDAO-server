using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Ranking.Provider
{
    public interface IRankingAppPointsCalcProvider
    {
        public long CalculatePointsFromPointsType(PointsType? pointsType, long count = 0);
        public long CalculatePointsFromReferralVotes(long voteCount);
        public long CalculatePointsFromReferralTopInviter();
        public long CalculatePointsFromVotes(long voteCount);
        public long CalculatePointsFromLikes(long likeCount);
        public long CalculateVotesFromPoints(long votePoints);
        public long CalculatePointsFromDailyViewAsset();
        public long CalculatePointsFromDailyFirstInvite();
        public long CalculatePointsFromExploreJoinTgChannel();
        public long CalculatePointsFromExploreFollowX();
        public long CalculatePointsFromExploreJoinDiscord();
        public long CalculatePointsFromExploreCumulateFiveInvite();
        public long CalculatePointsFromExploreCumulateTenInvite();
        public long CalculatePointsFromExploreCumulateTwentyInvite();
        public long CalculatePointsFromPointsExploreForwardX();
        public long CalculatePointsFromDailyViewAds();
    }

    public class RankingAppPointsCalcProvider : IRankingAppPointsCalcProvider, ISingletonDependency
    {
        private readonly ILogger<RankingAppPointsCalcProvider> _logger;
        private readonly IOptionsMonitor<RankingOptions> _rankingOptions;

        public RankingAppPointsCalcProvider(ILogger<RankingAppPointsCalcProvider> logger,
            IOptionsMonitor<RankingOptions> rankingOptions)
        {
            _logger = logger;
            _rankingOptions = rankingOptions;
        }

        public long CalculatePointsFromDailyViewAsset()
        {
            return _rankingOptions.CurrentValue.PointsDailyViewAsset;
        }

        public long CalculatePointsFromDailyFirstInvite()
        {
            return _rankingOptions.CurrentValue.PointsDailyFirstInvite;
        }

        public long CalculatePointsFromExploreJoinTgChannel()
        {
            return _rankingOptions.CurrentValue.PointsExploreJoinTgChannel;
        }

        public long CalculatePointsFromExploreFollowX()
        {
            return _rankingOptions.CurrentValue.PointsExploreFollowX;
        }

        public long CalculatePointsFromExploreJoinDiscord()
        {
            return _rankingOptions.CurrentValue.PointsExploreJoinDiscord;
        }

        public long CalculatePointsFromExploreCumulateFiveInvite()
        {
            return _rankingOptions.CurrentValue.PointsExploreCumulateFiveInvite;
        }

        public long CalculatePointsFromExploreCumulateTenInvite()
        {
            return _rankingOptions.CurrentValue.PointsExploreCumulateTenInvite;
        }

        public long CalculatePointsFromExploreCumulateTwentyInvite()
        {
            return _rankingOptions.CurrentValue.PointsExploreCumulateTwentyInvite;
        }
        
        public long CalculatePointsFromPointsExploreForwardX()
        {
            return _rankingOptions.CurrentValue.PointsExploreForwardX;
        }

        public long CalculatePointsFromDailyViewAds()
        {
            return _rankingOptions.CurrentValue.PointsViewAd;
        }
        
        public long CalculatePointsFromDailyCreatePoll()
        {
            return _rankingOptions.CurrentValue.PointsDailyCreatePoll;
        }
        
        public long CalculatePointsFromExploreJoinVotigram()
        {
            return _rankingOptions.CurrentValue.PointsExploreJoinVotigram;
        }
        
        public long CalculatePointsFromExploreFollowVotigramX()
        {
            return _rankingOptions.CurrentValue.PointsExploreFollowVotigramX;
        }
        
        public long CalculatePointsFromExploreForwardVotigramX()
        {
            return _rankingOptions.CurrentValue.PointsExploreForwardVotigramX;
        }
        
        public long CalculatePointsFromExploreSchrodinger()
        {
            return _rankingOptions.CurrentValue.PointsExploreSchrodinger;
        }

        public long CalculatePointsFromPointsType(PointsType? pointsType, long count = 0)
        {
            return pointsType switch
            {
                PointsType.Vote => CalculatePointsFromVotes(count),
                PointsType.Like => CalculatePointsFromLikes(count),
                PointsType.InviteVote or PointsType.BeInviteVote => CalculatePointsFromReferralVotes(count),
                PointsType.TopInviter => CalculatePointsFromReferralTopInviter(),
                PointsType.DailyViewAsset => CalculatePointsFromDailyViewAsset(),
                PointsType.DailyFirstInvite => CalculatePointsFromDailyFirstInvite(),
                PointsType.ExploreJoinTgChannel => CalculatePointsFromExploreJoinTgChannel(),
                PointsType.ExploreFollowX => CalculatePointsFromExploreFollowX(),
                PointsType.ExploreJoinDiscord => CalculatePointsFromExploreJoinDiscord(),
                PointsType.ExploreCumulateFiveInvite => CalculatePointsFromExploreCumulateFiveInvite(),
                PointsType.ExploreCumulateTenInvite => CalculatePointsFromExploreCumulateTenInvite(),
                PointsType.ExploreCumulateTwentyInvite => CalculatePointsFromExploreCumulateTwentyInvite(),
                PointsType.ExploreForwardX => CalculatePointsFromPointsExploreForwardX(),
                PointsType.DailyViewAds => CalculatePointsFromDailyViewAds(),
                PointsType.DailyCreatePoll => CalculatePointsFromDailyCreatePoll(),
                PointsType.ExploreJoinVotigram => CalculatePointsFromExploreJoinVotigram(),
                PointsType.ExploreFollowVotigramX => CalculatePointsFromExploreFollowVotigramX(),
                PointsType.ExploreForwardVotigramX => CalculatePointsFromExploreForwardVotigramX(),
                PointsType.ExploreSchrodinger => CalculatePointsFromExploreSchrodinger(),
                _ => 0
            };
        }

        public long CalculatePointsFromReferralVotes(long voteCount)
        {
            return _rankingOptions.CurrentValue.PointsFirstReferralVote * voteCount;
        }
        
        public long CalculatePointsFromReferralTopInviter()
        {
            return _rankingOptions.CurrentValue.PointsReferralTopInviter;
        }

        public long CalculatePointsFromVotes(long voteCount)
        {
            return _rankingOptions.CurrentValue.PointsPerVote * voteCount;
        }

        public long CalculatePointsFromLikes(long likeCount)
        {
            return _rankingOptions.CurrentValue.PointsPerLike * likeCount;
        }

        public long CalculateVotesFromPoints(long votePoints)
        {
            return votePoints / _rankingOptions.CurrentValue.PointsPerVote;
        }
    }
}