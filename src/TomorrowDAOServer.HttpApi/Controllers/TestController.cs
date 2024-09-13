using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Options;
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
    private readonly IOptionsMonitor<EmojiOptions> _emojiOptions;

    public TestController(IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, 
        IDistributedCache<string> distributedCache, IOptionsMonitor<EmojiOptions> emojiOptions)
    {
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _distributedCache = distributedCache;
        _emojiOptions = emojiOptions;
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
    
    [HttpGet("emoji")]
    public string Emoji()
    {
        return _emojiOptions.CurrentValue.Smile;
    }
    
}