using System;

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
    private const string DistributedCacheOpenedAppCountPrefix = "Count:OpenedApp";
    private const string DistributedCacheLikedAppCountPrefix = "Count:LikedApp";
    private const string DistributedCacheProposalVotePointsPrefix = "Proposal:VotePoints";
    private const string DistributedCacheProposalLikePointsPrefix = "Proposal:LikePoints";
    private const string DistributedCacheTotalPointsPrefix = "AllApps:Points";
    private const string DistributedCacheTotalVotesPrefix = "AllApps:Vote";
    private const string DistributedCacheTotalLikesPrefix = "AllApps:Like";


    public static string GenerateDistributeCacheKey(string chainId, string address, string proposalId, string category)
    {
        return category.IsNullOrWhiteSpace()
            ? $"{DistributedCachePrefix}:{chainId}:{address}:{proposalId}"
            : $"{DistributedCachePrefix}:{chainId}:{address}:{proposalId}:{category}";
    }

    public static string GenerateDistributedLockKey(string chainId, string address, string proposalId)
    {
        return $"{DistributedLockPrefix}:{chainId}:{address}:{proposalId}";
    }

    public static string GenerateAppPointsVoteCacheKey(string proposalId, string alias)
    {
        return $"{DistributedCachePointsVotePrefix}:{proposalId}:{alias}";
    }

    public static string GenerateRankingAppPointsLikeCacheKey(string proposalId, string alias)
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

    public static string GenerateProposalVotePointsCacheKey(string proposalId)
    {
        return $"{DistributedCacheProposalVotePointsPrefix}:{proposalId}";
    }
    
    public static string GenerateProposalLikePointsCacheKey(string proposalId)
    {
        return $"{DistributedCacheProposalLikePointsPrefix}:{proposalId}";
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

    public static string GenerateOpenedAppCountCacheKey(string alias)
    {
        return $"{DistributedCacheOpenedAppCountPrefix}:{alias}";
    }
    
    public static string GenerateLikedAppCountCacheKey(string alias)
    {
        return $"{DistributedCacheLikedAppCountPrefix}:{alias}";
    }
    
    public static string GenerateTotalPointsCacheKey()
    {
        return $"{DistributedCacheTotalPointsPrefix}";
    }
    
    public static string GenerateTotalVotesCacheKey()
    {
        return $"{DistributedCacheTotalVotesPrefix}";
    }
    
    public static string GenerateTotalLikesCacheKey()
    {
        return $"{DistributedCacheTotalLikesPrefix}";
    }
}