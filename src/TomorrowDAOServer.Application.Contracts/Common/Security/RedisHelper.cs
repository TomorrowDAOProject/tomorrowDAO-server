namespace TomorrowDAOServer.Common.Security;

public class RedisHelper
{
    private const string DistributedLockPrefix = "RankingVote";
    private const string DistributedCachePrefix = "RankingVotingRecord";
    private const string DistributedCacheDefaultProposalPrefix = "DefaultProposal";
    private const string DistributedCachePointsVotePrefix = "Points:Vote";
    private const string DistributedCachePointsLikePrefix = "Points:Like";
    private const string DistributedCachePointsAllPrefix = "Points:All";
    private const string DistributedCachePointsLoginPrefix = "Points:Login";
    
    
    public static string GenerateDistributeCacheKey(string chainId, string address, string proposalId)
    {
        return $"{DistributedCachePrefix}:{chainId}:{address}:{proposalId}";
    }

    public static string GenerateDistributedLockKey(string chainId, string address, string proposalId)
    {
        return $"{DistributedLockPrefix}:{chainId}:{address}:{proposalId}";
    }

    public static string GenerateAppPointsVoteCacheKey(string proposalId, string alias)
    {
        return $"{DistributedCachePointsVotePrefix}:{proposalId}:{alias}";
    }
    
    public static string GenerateAppPointsLikeCacheKey(string proposalId, string alias)
    {
        return $"{DistributedCachePointsLikePrefix}:{proposalId}:{alias}";
    }
    
    public static string GenerateUserPointsAllCacheKey(string address)
    {
        return $"{DistributedCachePointsAllPrefix}:{address}";
    }

    public static string GenerateUserAllPointsByIdCacheKey(string userId)
    {
        return $"{DistributedCachePointsAllPrefix}:{userId}";
    }
    
    public static string GenerateDefaultProposalCacheKey(string chainId)
    {
        return $"{DistributedCacheDefaultProposalPrefix}:{chainId}";
    }

    public static string GenerateUserLoginPointsCacheKey(string address)
    {
        return $"{DistributedCachePointsLoginPrefix}:{address}";
    }
    public static string GenerateUserLoginPointsByIdCacheKey(string userId)
    {
        return $"{DistributedCachePointsLoginPrefix}:{userId}";
    }
}