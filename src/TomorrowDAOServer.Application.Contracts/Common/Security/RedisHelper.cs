namespace TomorrowDAOServer.Common.Security;

public class RedisHelper
{
    private const string DistributedLockPrefix = "RankingVote";
    private const string DistributedCachePrefix = "RankingVotingRecord";
    private const string DistributedCacheDefaultProposalPrefix = "DefaultProposal";
    private const string DistributedCachePointsVotePrefix = "Points:Vote";
    private const string DistributedCachePointsLikePrefix = "Points:Like";
    private const string DistributedCachePointsAllPrefix = "Points:All";
    
    
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
    
    public static string GenerateDefaultProposalCacheKey(string chainId)
    {
        return $"{DistributedCacheDefaultProposalPrefix}:{chainId}";
    }
}