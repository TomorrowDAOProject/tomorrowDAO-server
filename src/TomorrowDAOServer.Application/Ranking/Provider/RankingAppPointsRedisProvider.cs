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
    Task<Dictionary<string, string>> MultiGetAsync(List<string> keys);
    Task<string> GetAsync(string key);
    Task IncrementAsync(string key, long amount);
    Task<List<RankingAppPointsDto>> GetAllAppPointsAsync(string chainId, string proposalId);
    Task<List<RankingAppPointsDto>> GetDefaultAllAppPointsAsync(string chainId);
    Task<long> GetUserAllPointsAsync(string address);
    Task IncrementPointsAsync(RankingAppLikeInput likeInfo, string address, long points);
    Task<long> AddOrUpdateAppAndUserPointsAsync(string chainId, string proposalId, string address, string alias, long points);
    Task IncrementAppPointsAsync(string proposalId, string alias, long points);
    Task IncrementUserPointsAsync(string proposalId, string address, string alias, long points);
    
}

public class RankingAppPointsRedisProvider : IRankingAppPointsRedisProvider, ISingletonDependency
{
    private readonly ILogger<RankingAppPointsRedisProvider> _logger;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly ConnectionMultiplexer _redis;

    public RankingAppPointsRedisProvider(ILogger<RankingAppPointsRedisProvider> logger, 
        IRankingAppProvider rankingAppProvider, IProposalProvider proposalProvider, ConnectionMultiplexer redis)
    {
        _logger = logger;
        _rankingAppProvider = rankingAppProvider;
        _proposalProvider = proposalProvider;
        _redis = redis;
    }

    public async Task<Dictionary<string, string>> MultiGetAsync(List<string> keys)
    {
        if (keys.IsNullOrEmpty())
        {
            return new Dictionary<string, string>();
        }
        var database = _redis.GetDatabase();
        var redisKeys = keys.Select(x => (RedisKey)x).ToArray();
        var values = await database.StringGetAsync(redisKeys);

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
        var database = _redis.GetDatabase();
        return await database.StringGetAsync(key);
    }

    public async Task IncrementAsync(string key, long amount)
    {
        var database = _redis.GetDatabase();
        await database.StringIncrementAsync(key, amount);
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

    public Task IncrementPointsAsync(RankingAppLikeInput likeInfo, string address, long points)
    {
        throw new NotImplementedException();
    }

    public Task<long> AddOrUpdateAppAndUserPointsAsync(string chainId, string proposalId, string address, string alias, long points)
    {
        throw new NotImplementedException();
    }

    public Task IncrementAppPointsAsync(string proposalId, string alias, long points)
    {
        throw new NotImplementedException();
    }

    public Task IncrementUserPointsAsync(string proposalId, string address, string alias, long points)
    {
        throw new NotImplementedException();
    }
}