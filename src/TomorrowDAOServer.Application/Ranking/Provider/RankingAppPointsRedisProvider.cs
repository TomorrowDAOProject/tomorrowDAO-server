using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Security;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Ranking.Provider;

public interface IRankingAppPointsRedisProvider
{
    public Task SetAsync(string key, string value, TimeSpan? expire = null);
    Task<Dictionary<string, string>> MultiGetAsync(List<string> keys);
    Task<string> GetAsync(string key);
    Task IncrementAsync(string key, long amount);
    Task<List<RankingAppPointsDto>> GetAllAppPointsAsync(string chainId, string proposalId);
    Task<List<RankingAppPointsDto>> GetDefaultAllAppPointsAsync(string chainId);
    Task<long> GetUserAllPointsAsync(string address);
    Task IncrementLikePointsAsync(RankingAppLikeInput likeInput, string address);
    Task IncrementVotePointsAsync(string chainId, string proposalId, string address, string alias, long voteAmount);
}

public class RankingAppPointsRedisProvider : IRankingAppPointsRedisProvider, ISingletonDependency
{
    private readonly ILogger<RankingAppPointsRedisProvider> _logger;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IDatabase _database;
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;

    public RankingAppPointsRedisProvider(ILogger<RankingAppPointsRedisProvider> logger, 
        IRankingAppProvider rankingAppProvider, IProposalProvider proposalProvider,
        IConnectionMultiplexer connectionMultiplexer, IRankingAppPointsCalcProvider rankingAppPointsCalcProvider)
    {
        _logger = logger;
        _rankingAppProvider = rankingAppProvider;
        _proposalProvider = proposalProvider;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
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

    public async Task<List<RankingAppPointsDto>> GetAllAppPointsAsync(string chainId, string proposalId)
    {
        var rankingAppList = await _rankingAppProvider.GetByProposalIdAsync(chainId, proposalId);
        if (rankingAppList.IsNullOrEmpty())
        {
            return new List<RankingAppPointsDto>();
        }
        
        var cacheKeys = rankingAppList.SelectMany(index => new[]
        {
            RedisHelper.GenerateAppPointsVoteCacheKey(index.ProposalId, index.Alias), 
            RedisHelper.GenerateAppPointsLikeCacheKey(index.ProposalId, index.Alias)
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
                    Points = Convert.ToInt64(pair.Value),
                    PointsType = Enum.TryParse<PointsType>(keyParts[1], out var parsedPointsType) ? 
                        parsedPointsType : 
                        PointsType.Vote
                };
            })
            .ToList();
    }

    public async Task<List<RankingAppPointsDto>> GetDefaultAllAppPointsAsync(string chainId)
    {
        var defaultProposal = await _proposalProvider.GetDefaultProposalAsync(chainId);
        if (defaultProposal == null)
        {
            return new List<RankingAppPointsDto>();
        }

        return await GetAllAppPointsAsync(chainId, defaultProposal.ProposalId);
    }

    public async Task<long> GetUserAllPointsAsync(string address)
    {
        var cacheKey = RedisHelper.GenerateUserPointsAllCacheKey(address);
        var cache = await GetAsync(cacheKey);
        return cache.IsNullOrWhiteSpace() ? 0 : Convert.ToInt64(cache);
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
}