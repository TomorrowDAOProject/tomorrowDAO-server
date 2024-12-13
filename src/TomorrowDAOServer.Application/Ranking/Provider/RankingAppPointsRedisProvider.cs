using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nest;
using StackExchange.Redis;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Security;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Dto;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Ranking.Provider;

public interface IRankingAppPointsRedisProvider
{
    public Task SetAsync(string key, string value, TimeSpan? expire = null);
    Task<Dictionary<string, string>> MultiGetAsync(List<string> keys);
    Task<string> GetAsync(string key);
    Task<long> IncrementAsync(string key, long amount);
    Task<List<RankingAppPointsDto>> GetAllAppPointsAsync(string chainId, string proposalId, List<string> aliasList);
    Task<List<RankingAppPointsDto>> GetDefaultAllAppPointsAsync(string chainId);
    Task<long> GetUserAllPointsAsync(string address);
    Task<long> GetUserAllPointsByIdAsync(string userId);
    Task<long> GetUserAllPointsAsync(string userId, string address);
    Task<Dictionary<string, long>> IncrementLikePointsAsync(RankingAppLikeInput likeInput, string address);
    Task<Tuple<string, long>> GetRankingLikePointAsync(string proposalId, string alias);
    Task IncrementVotePointsAsync(string chainId, string proposalId, string address, string alias, long voteAmount);
    Task<Tuple<string, long>> GetRankingVotePointsAsync(string proposalId, string alias);
    Task IncrementReferralVotePointsAsync(string inviter, string invitee, long voteCount);
    Task IncrementReferralTopInviterPointsAsync(string address);
    Task IncrementTaskPointsAsync(string address, UserTaskDetail userTaskDetail);
    Task IncrementViewAdPointsAsync(string address);
    Task<Tuple<string, List<string>>> GetDefaultRankingProposalInfoAsync(string chainId);
    Task<string> GetDefaultRankingProposalIdAsync(string chainId);
    Task GenerateRedisDefaultProposal(string proposalId, string proposalDesc, string chainId);
    //Login points
    Task IncrementLoginPointsAsync(string address, bool viewAd, int consecutiveLoginDays);
    Task IncrementLoginPointsByUserIdAsync(string userId, bool viewAd, int consecutiveLoginDays);
    Task<long> IncrementOpenedAppCountAsync(string alias, int count);
    Task<Dictionary<string, long>> GetOpenedAppCountAsync(List<string> alias);
    Task<Dictionary<string, long>> GetAppLikeCountAsync(List<string> aliases);
}

public class RankingAppPointsRedisProvider : IRankingAppPointsRedisProvider, ISingletonDependency
{
    private readonly ILogger<RankingAppPointsRedisProvider> _logger;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IDatabase _database;
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;
    private readonly IDistributedCache<string> _distributedCache;

    public RankingAppPointsRedisProvider(ILogger<RankingAppPointsRedisProvider> logger, 
        IRankingAppProvider rankingAppProvider, IProposalProvider proposalProvider,
        IConnectionMultiplexer connectionMultiplexer, IRankingAppPointsCalcProvider rankingAppPointsCalcProvider,
        IDistributedCache<string> distributedCache)
    {
        _logger = logger;
        _rankingAppProvider = rankingAppProvider;
        _proposalProvider = proposalProvider;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
        _distributedCache = distributedCache;
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task SetAsync(string key, string value, TimeSpan? expire = null)
    {
        await _database.StringSetAsync(key, value);
        if (expire != null)
        {
            _database.KeyExpire(key, expire);
        }
    }

    public async Task<Dictionary<string, string>> MultiGetAsync(List<string> keys)
    {
        if (keys.IsNullOrEmpty())
        {
            return new Dictionary<string, string>();
        }
        var redisKeys = keys.Select(x => (RedisKey)x).ToArray();
        var values = await _database.StringGetAsync(redisKeys);

        var result = keys
            .Zip(values, (k, v) => new KeyValuePair<string, string>(k, v.IsNull ? string.Empty : v.ToString()))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        return result;
    }

    public async Task<string> GetAsync(string key)
    {
        if (key.IsNullOrEmpty())
        {
            return string.Empty;
        }
        return await _database.StringGetAsync(key);
    }

    public async Task<long> IncrementAsync(string key, long amount)
    {
        return await _database.StringIncrementAsync(key, amount);
    }

    public async Task<List<RankingAppPointsDto>> GetAllAppPointsAsync(string chainId, string proposalId, List<string> aliasList)
    {
        var cacheKeys = aliasList.SelectMany(alias => new[]
        {
            RedisHelper.GenerateAppPointsVoteCacheKey(proposalId, alias), 
            RedisHelper.GenerateRankingAppPointsLikeCacheKey(proposalId, alias)
        }).ToList();
        var pointsDic = await MultiGetAsync(cacheKeys);
        return pointsDic
            .Select(pair =>
            {
                var keyParts = pair.Key.Split(CommonConstant.Colon);
                return new RankingAppPointsDto
                {
                    ProposalId = keyParts[2],
                    Alias = keyParts[3],
                    Points = long.TryParse(pair.Value, out var points) ? points : 0,
                    PointsType = Enum.TryParse<PointsType>(keyParts[1], out var parsedPointsType) ? 
                        parsedPointsType : 
                        PointsType.All
                };
            })
            .ToList();
    }

    public async Task<List<RankingAppPointsDto>> GetDefaultAllAppPointsAsync(string chainId)
    {
        var (proposalId, aliasList) = await GetDefaultRankingProposalInfoAsync(chainId);
        if (proposalId.IsNullOrEmpty() || aliasList.IsNullOrEmpty())
        {
            return new List<RankingAppPointsDto>();
        }

        return await GetAllAppPointsAsync(chainId, proposalId, aliasList);
    }

    public async Task<long> GetUserAllPointsAsync(string address)
    {
        var cacheKey = RedisHelper.GenerateUserPointsAllCacheKey(address);
        var cache = await GetAsync(cacheKey);
        return long.TryParse(cache, out var points) ? points : 0;
    }

    public async Task<long> GetUserAllPointsByIdAsync(string userId)
    {
        var cacheKey = RedisHelper.GenerateUserAllPointsByIdCacheKey(userId);
        var cache = await GetAsync(cacheKey);
        return long.TryParse(cache, out var points) ? points : 0;
    }
    
    public async Task<long> GetUserAllPointsAsync(string userId, string address)
    {
        var totalPoints = 0L;
        if (!address.IsNullOrWhiteSpace())
        {
            totalPoints += await GetUserAllPointsAsync(address);
        }

        if (!userId.IsNullOrWhiteSpace())
        {
            totalPoints += await GetUserAllPointsByIdAsync(userId);
        }
        
        return totalPoints;
    }

    public async Task<Dictionary<string, long>> IncrementLikePointsAsync(RankingAppLikeInput likeInput, string address)
    {
        var likeList = likeInput.LikeList;
        var proposalId = likeInput.ProposalId ?? string.Empty;

        var taskList = new List<Task>();
        var aliasIncrementMap = new List<Task<(string Alias, long Points)>>();
        foreach (var like in likeList)
        {
            if (!likeInput.ProposalId.IsNullOrWhiteSpace())
            {
                var rankingAppLikeKey = RedisHelper.GenerateRankingAppPointsLikeCacheKey(proposalId, like.Alias);
                var proposalKey = RedisHelper.GenerateProposalLikePointsCacheKey(proposalId);
                var rankingAppLikePoints = _rankingAppPointsCalcProvider.CalculatePointsFromLikes(like.LikeAmount);
                taskList.Add(IncrementAsync(rankingAppLikeKey, rankingAppLikePoints));
                taskList.Add(IncrementAsync(proposalKey, rankingAppLikePoints));
            }
            
            var appLikeKey = RedisHelper.GenerateLikedAppCountCacheKey(like.Alias);
            aliasIncrementMap.Add(
                IncrementAsync(appLikeKey, like.LikeAmount).ContinueWith(t => (like.Alias, t.Result))
            );
        }

        var userKey = RedisHelper.GenerateUserPointsAllCacheKey(address);
        var totalLikesKey = RedisHelper.GenerateTotalLikesCacheKey();
        var totalPointsKey = RedisHelper.GenerateTotalPointsCacheKey();
        var userLikePoints = _rankingAppPointsCalcProvider.CalculatePointsFromLikes(likeList.Sum(x => x.LikeAmount));
        taskList.Add(IncrementAsync(userKey, userLikePoints));
        taskList.Add(IncrementAsync(totalLikesKey, userLikePoints));
        taskList.Add(IncrementAsync(totalPointsKey, userLikePoints));

        await Task.WhenAll(taskList);
        var aliasResults = await Task.WhenAll(aliasIncrementMap);
        var aliasLikeCountDic = new Dictionary<string, long>();
        foreach (var (alias, points) in aliasResults)
        {
            aliasLikeCountDic[alias] = points;
        }

        return aliasLikeCountDic;
    }

    public async Task<Tuple<string, long>> GetRankingLikePointAsync(string proposalId, string alias)
    {
        var appLikeKey =  RedisHelper.GenerateRankingAppPointsLikeCacheKey(proposalId, alias);
        var pointsString = await GetAsync(appLikeKey);
        var points = long.TryParse(pointsString, out var pts) ? pts : 0;
        return new Tuple<string, long>(appLikeKey, points);
    }

    public async Task IncrementVotePointsAsync(string chainId, string proposalId, string address, string alias, long voteAmount)
    {
        var appVoteKey = RedisHelper.GenerateAppPointsVoteCacheKey(proposalId, alias);
        var userKey = RedisHelper.GenerateUserPointsAllCacheKey(address);
        var proposalKey = RedisHelper.GenerateProposalVotePointsCacheKey(proposalId);
        var totalVotesKey = RedisHelper.GenerateTotalVotesCacheKey();
        var totalPointsKey = RedisHelper.GenerateTotalPointsCacheKey();
        var votePoints = _rankingAppPointsCalcProvider.CalculatePointsFromVotes(voteAmount);
        await Task.WhenAll(IncrementAsync(appVoteKey, votePoints), 
            IncrementAsync(userKey, votePoints), IncrementAsync(proposalKey, votePoints),
            IncrementAsync(totalVotesKey, votePoints), IncrementAsync(totalPointsKey, votePoints));
    }
    
    public async Task<Tuple<string, long>> GetRankingVotePointsAsync(string proposalId, string alias)
    {
        var appVoteKey = RedisHelper.GenerateAppPointsVoteCacheKey(proposalId, alias);
        var pointsString = await GetAsync(appVoteKey);
        var points = long.TryParse(pointsString, out var pts) ? pts : 0;
        return new Tuple<string, long>(appVoteKey, points);
    }

    public async Task IncrementReferralVotePointsAsync(string inviter, string invitee, long voteCount)
    {
        var inviterUserKey = RedisHelper.GenerateUserPointsAllCacheKey(inviter);
        var inviteeUserKey = RedisHelper.GenerateUserPointsAllCacheKey(invitee);
        var referralVotePoints = _rankingAppPointsCalcProvider.CalculatePointsFromReferralVotes(voteCount);
        await Task.WhenAll(IncrementAsync(inviterUserKey, referralVotePoints), IncrementAsync(inviteeUserKey, referralVotePoints));
    }

    public async Task IncrementReferralTopInviterPointsAsync(string address)
    {
        var userKey = RedisHelper.GenerateUserPointsAllCacheKey(address);
        var referralTopInviterPoints = _rankingAppPointsCalcProvider.CalculatePointsFromReferralTopInviter();
        await IncrementAsync(userKey, referralTopInviterPoints);
    }

    public async Task IncrementTaskPointsAsync(string address, UserTaskDetail userTaskDetail)
    {
        var userKey = RedisHelper.GenerateUserPointsAllCacheKey(address);
        var pointsType = TaskPointsHelper.GetPointsTypeFromUserTaskDetail(userTaskDetail);
        var taskPoints = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(pointsType);
        await IncrementAsync(userKey, taskPoints);
    }

    public async Task IncrementViewAdPointsAsync(string address)
    {
        var userKey = RedisHelper.GenerateUserPointsAllCacheKey(address);
        var viewAdPoints = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.DailyViewAds);
        await IncrementAsync(userKey, viewAdPoints);
    }

    public async Task<Tuple<string, List<string>>> GetDefaultRankingProposalInfoAsync(string chainId)
    {
        var distributeCacheKey = RedisHelper.GenerateDefaultProposalCacheKey(chainId);
        var value = await _distributedCache.GetAsync(distributeCacheKey);
        if (value.IsNullOrEmpty())
        {
            return new Tuple<string, List<string>>(string.Empty, new List<string>());
        }

        var valueParts = value.Split(CommonConstant.Comma);
        if (valueParts.Length <= 0)
        {
            return new Tuple<string, List<string>>(string.Empty, new List<string>());
        }

        var proposalId = valueParts[0];
        var aliasList = valueParts.Skip(1).ToList();
        return new Tuple<string, List<string>>(proposalId, aliasList);

    }

    public async Task<string> GetDefaultRankingProposalIdAsync(string chainId)
    {
        var (proposalId, _) = await GetDefaultRankingProposalInfoAsync(chainId);
        return proposalId;
    }
    
    public async Task GenerateRedisDefaultProposal(string proposalId, string proposalDesc, string chainId)
    {
        var aliasString = RankHelper.GetAliasString(proposalDesc);
        var value = proposalId + CommonConstant.Comma + aliasString;
        var distributeCacheKey = RedisHelper.GenerateDefaultProposalCacheKey(chainId);
        await _distributedCache.SetAsync(distributeCacheKey, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(87600),
        });
    }

    public async Task IncrementLoginPointsAsync(string address, bool viewAd, int consecutiveLoginDays)
    {
        var key = RedisHelper.GenerateUserLoginPointsCacheKey(address);
        var loginPoints = _rankingAppPointsCalcProvider.CalculatePointsFromLogin(consecutiveLoginDays);
        if (viewAd)
        {
            loginPoints *= 2;
        }
        await IncrementAsync(key, loginPoints);
        
        var userKey = RedisHelper.GenerateUserPointsAllCacheKey(address);
        await IncrementAsync(userKey, loginPoints);
    }

    public async Task IncrementLoginPointsByUserIdAsync(string userId, bool viewAd, int consecutiveLoginDays)
    {
        var key = RedisHelper.GenerateUserLoginPointsByIdCacheKey(userId);
        var loginPoints = _rankingAppPointsCalcProvider.CalculatePointsFromLogin(consecutiveLoginDays);
        if (viewAd)
        {
            loginPoints *= 2;
        }
        await IncrementAsync(key, loginPoints);
        
        var userKey = RedisHelper.GenerateUserAllPointsByIdCacheKey(userId);
        await IncrementAsync(userKey, loginPoints);
    }

    public async Task<long> IncrementOpenedAppCountAsync(string alias, int count)
    {
        var key = RedisHelper.GenerateOpenedAppCountCacheKey(alias);
        var increment = await IncrementAsync(key, count);
        return increment;
    }

    public async Task<Dictionary<string, long>> GetOpenedAppCountAsync(List<string> aliases)
    {
        if (aliases.IsNullOrEmpty())
        {
            return new Dictionary<string, long>();
        }

        var keyAliasMap = aliases.ToDictionary(
            alias => RedisHelper.GenerateOpenedAppCountCacheKey(alias), 
            alias => alias
        );

        var batch = _database.CreateBatch();
        var tasks = new Dictionary<string, Task<RedisValue>>();
        foreach (var key in keyAliasMap.Keys)
        {
            tasks[key] = batch.StringGetAsync(key);
        }
        batch.Execute();
        await Task.WhenAll(tasks.Values);
        return tasks.ToDictionary(
            kvp => keyAliasMap[kvp.Key],
            kvp => (long)(kvp.Value.Result.HasValue ? (long)kvp.Value.Result : 0)
        );
    }
    
    public async Task<Dictionary<string, long>> GetAppLikeCountAsync(List<string> aliases)
    {
        if (aliases.IsNullOrEmpty())
        {
            return new Dictionary<string, long>();
        }

        var keyAliasMap = aliases.ToDictionary(
            alias => RedisHelper.GenerateLikedAppCountCacheKey(alias), 
            alias => alias
        );

        var batch = _database.CreateBatch();
        var tasks = new Dictionary<string, Task<RedisValue>>();
        foreach (var key in keyAliasMap.Keys)
        {
            tasks[key] = batch.StringGetAsync(key);
        }
        batch.Execute();
        await Task.WhenAll(tasks.Values);
        return tasks.ToDictionary(
            kvp => keyAliasMap[kvp.Key],
            kvp => (long)(kvp.Value.Result.HasValue ? (long)kvp.Value.Result : 0)
        );
    }
}