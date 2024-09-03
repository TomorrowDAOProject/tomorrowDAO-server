using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Ranking.Provider
{
    public interface IRankingAppPointsCalcProvider
    {
        public long CalculatePointsFromVotes(long voteCount);
        public long CalculatePointsFromLikes(long likeCount);
        public long CalculateVotesFromPoints(long votePoints);
    }

    public class RankingAppPointsCalcProvider : IRankingAppPointsCalcProvider, ISingletonDependency
    {
        private ILogger<RankingAppPointsCalcProvider> _logger;
        private IOptionsMonitor<RankingOptions> _rankingOptions;

        public RankingAppPointsCalcProvider(ILogger<RankingAppPointsCalcProvider> logger,
            IOptionsMonitor<RankingOptions> rankingOptions)
        {
            _logger = logger;
            _rankingOptions = rankingOptions;
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
            return votePoints / _rankingOptions.CurrentValue.PointsPerLike;
        }
    }
}