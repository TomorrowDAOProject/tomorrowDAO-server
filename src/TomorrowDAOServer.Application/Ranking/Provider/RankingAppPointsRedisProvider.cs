using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    Task<List<RankingAppPointsDto>> GetAllAppPointsAsync(string chainId, string proposalId);
    Task<List<RankingAppPointsDto>> GetDefaultAllAppPointsAsync(string chainId);
    Task<long> GetUserAllPointsAsync(string address);
    Task IncrementPoints(RankingAppLikeInput likeInfo, string address, long points);
    Task<long> AddOrUpdateAppAndUserPointsAsync(string chainId, string proposalId, string address, string alias, long points);
    Task IncrementAppPointsAsync(string proposalId, string alias, long points);
    Task IncrementUserPointsAsync(string proposalId, string address, string alias, long points);
    
}

public class RankingAppPointsRedisProvider : IRankingAppPointsRedisProvider, ISingletonDependency
{
    private readonly ILogger<RankingAppPointsRedisProvider> _logger;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IProposalProvider _proposalProvider;

    public RankingAppPointsRedisProvider(ILogger<RankingAppPointsRedisProvider> logger, 
        IRankingAppProvider rankingAppProvider, IDistributedCache<string> distributedCache, 
        IProposalProvider proposalProvider)
    {
        _logger = logger;
        _rankingAppProvider = rankingAppProvider;
        _distributedCache = distributedCache;
        _proposalProvider = proposalProvider;
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
        var pointsDic = await _distributedCache.GetManyAsync(cacheKeys);
        
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
        var cache = await _distributedCache.GetAsync(cacheKey);
        return cache.IsNullOrWhiteSpace() ? 0 : Convert.ToInt64(cache);
    }

    public Task IncrementPoints(RankingAppLikeInput likeInfo, string address, long points)
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