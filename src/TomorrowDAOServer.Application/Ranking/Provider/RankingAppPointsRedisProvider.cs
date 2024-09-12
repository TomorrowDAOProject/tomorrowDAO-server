using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
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
    Task IncrementAsync(string key, long amount);
    Task<List<RankingAppPointsDto>> GetAllAppPointsAsync(string chainId, string proposalId, List<string> aliasList);
    Task<List<RankingAppPointsDto>> GetDefaultAllAppPointsAsync(string chainId);
    Task<long> GetUserAllPointsAsync(string address);
    Task IncrementLikePointsAsync(RankingAppLikeInput likeInput, string address);
    Task IncrementVotePointsAsync(string chainId, string proposalId, string address, string alias, long voteAmount);
    Task IncrementReferralVotePointsAsync(string inviter, string invitee, long voteCount);
    Task SaveDefaultRankingProposalIdAsync(string chainId, string value, DateTime? expire);
    Task<Tuple<string, List<string>>> GetDefaultRankingProposalInfoAsync(string chainId);
    Task<string> GetDefaultRankingProposalIdAsync(string chainId);
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

    public async Task IncrementAsync(string key, long amount)
    {
        await _database.StringIncrementAsync(key, amount);
    }

    public async Task<List<RankingAppPointsDto>> GetAllAppPointsAsync(string chainId, string proposalId, List<string> aliasList)
    {
        var cacheKeys = aliasList.SelectMany(alias => new[]
        {
            RedisHelper.GenerateAppPointsVoteCacheKey(proposalId, alias), 
            RedisHelper.GenerateAppPointsLikeCacheKey(proposalId, alias)
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

    public async Task IncrementLikePointsAsync(RankingAppLikeInput likeInput, string address)
    {
        var likeList = likeInput.LikeList;
        var proposalId = likeInput.ProposalId;
        var incrementTasks = (from like in likeList let 
            appLikeKey = RedisHelper.GenerateAppPointsLikeCacheKey(proposalId, like.Alias) 
            let appLikePoints = _rankingAppPointsCalcProvider.CalculatePointsFromLikes(like.LikeAmount) 
            select IncrementAsync(appLikeKey, appLikePoints))
            .ToList();

        var userKey = RedisHelper.GenerateUserPointsAllCacheKey(address);
        var userLikePoints = _rankingAppPointsCalcProvider.CalculatePointsFromLikes(likeList.Sum(x => x.LikeAmount));
        incrementTasks.Add(IncrementAsync(userKey, userLikePoints));

        await Task.WhenAll(incrementTasks);
    }

    public async Task IncrementVotePointsAsync(string chainId, string proposalId, string address, string alias, long voteAmount)
    {
        var appVoteKey = RedisHelper.GenerateAppPointsVoteCacheKey(proposalId, alias);
        var userKey = RedisHelper.GenerateUserPointsAllCacheKey(address);
        var votePoints = _rankingAppPointsCalcProvider.CalculatePointsFromVotes(voteAmount);
        await Task.WhenAll(IncrementAsync(appVoteKey, votePoints), IncrementAsync(userKey, votePoints));
    }

    public async Task IncrementReferralVotePointsAsync(string inviter, string invitee, long voteCount)
    {
        var inviterUserKey = RedisHelper.GenerateUserPointsAllCacheKey(inviter);
        var inviteeUserKey = RedisHelper.GenerateUserPointsAllCacheKey(invitee);
        var referralVotePoints = _rankingAppPointsCalcProvider.CalculatePointsFromReferralVotes(voteCount);
        await Task.WhenAll(IncrementAsync(inviterUserKey, referralVotePoints), IncrementAsync(inviteeUserKey, referralVotePoints));
    }

    public async Task SaveDefaultRankingProposalIdAsync(string chainId, string value, DateTime? expire)
    {
        var distributeCacheKey = RedisHelper.GenerateDefaultProposalCacheKey(chainId);
        await _distributedCache.SetAsync(distributeCacheKey, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(87600),
        });
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
}