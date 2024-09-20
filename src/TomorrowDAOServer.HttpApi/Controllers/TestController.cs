using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Referral.Provider;
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
    private readonly IReferralInviteProvider _referralInviteProvider;

    public TestController(IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, 
        IDistributedCache<string> distributedCache, IOptionsMonitor<EmojiOptions> emojiOptions, 
        IReferralInviteProvider referralInviteProvider)
    {
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _distributedCache = distributedCache;
        _emojiOptions = emojiOptions;
        _referralInviteProvider = referralInviteProvider;
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
    
    [HttpGet("not-vote-invitee")]
    public async Task<ReferralInviteRelationIndex> NotVoteInviteeAsync(string chainId, string caHash)
    {
        return await _referralInviteProvider.GetByNotVoteInviteeCaHashAsync(chainId, caHash);
    }
}