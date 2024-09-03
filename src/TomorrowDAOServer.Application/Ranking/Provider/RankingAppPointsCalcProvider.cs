using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Ranking.Provider
{
    public interface IRankingAppPointsCalcProvider
    {
        public long CalculatePointsFromVotes(int voteCount);
        public long CalculatePointsFromLikes(int likeCount);
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

        public long CalculatePointsFromVotes(int voteCount)
        {
            return _rankingOptions.CurrentValue.PointsPerVote * voteCount;
        }

        public long CalculatePointsFromLikes(int likeCount)
        {
            return _rankingOptions.CurrentValue.PointsPerLike * likeCount;
        }
    }
}