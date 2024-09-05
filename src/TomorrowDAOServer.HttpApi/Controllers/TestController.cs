using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Ranking.Provider;
using Volo.Abp;
using Volo.Abp.Caching;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Ranking")]
[Route("api/app/test")]
public class TestController
{
    private IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;
    private readonly IDistributedCache<string> _distributedCache;

    public TestController(IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, 
        IDistributedCache<string> distributedCache)
    {
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _distributedCache = distributedCache;
    }
    
    [HttpGet("redis-value")]
    public async Task<string> GetRedisValueAsync(string key)
    {
        return await _rankingAppPointsRedisProvider.GetAsync(key);
    }
    
    [HttpGet("redis-value-distributed-cache")]
    public async Task<string> GetRedisValueDistributedCacheAsync(string key)
    {
        return await _distributedCache.GetAsync(key);
    }
    
    [HttpGet("default-proposal-id")]
    public async Task<string> GetDefaultRankingProposalIdAsync(string chainId)
    {
        return await _rankingAppPointsRedisProvider.GetDefaultRankingProposalIdAsync(chainId);
    }
    
}