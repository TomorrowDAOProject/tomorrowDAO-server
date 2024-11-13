using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Referral.Provider;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Caching;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Test")]
[Route("api/app/test")]
public class TestController
{
    private IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IOptionsMonitor<EmojiOptions> _emojiOptions;
    private readonly IReferralInviteProvider _referralInviteProvider;
    private readonly IUserProvider _userProvider;
    private readonly IUserService _userService;

    public TestController(IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, 
        IDistributedCache<string> distributedCache, IOptionsMonitor<EmojiOptions> emojiOptions, 
        IReferralInviteProvider referralInviteProvider, IUserProvider userProvider, IUserService userService)
    {
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _distributedCache = distributedCache;
        _emojiOptions = emojiOptions;
        _referralInviteProvider = referralInviteProvider;
        _userProvider = userProvider;
        _userService = userService;
    }
    
    [HttpGet("redis-value")]
    public async Task<string> GetRedisValueAsync(string key)
    {
        return await _rankingAppPointsRedisProvider.GetAsync(key);
    }
    
    [HttpGet("increment")]
    public async Task IncrementAsync(string key, long delta)
    {
        await _rankingAppPointsRedisProvider.IncrementAsync(key, delta);
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
    
    [HttpGet("get-and-validate-user-address")]
    public async Task<string> GetAndValidateUserAddressAsync(string userId, string chainId)
    {
        return await _userProvider.GetAndValidateUserAddressAsync(new Guid(userId), chainId);
    }
    
    [HttpGet("increment-invite-count")]
    public async Task<long> IncrementInviteCountAsync(string chainId, string address, long delta)
    {
        return await _referralInviteProvider.IncrementInviteCountAsync(chainId, address, delta);
    }
    
    [HttpGet("get-invite-count")]
    public async Task<long> GetInviteCountAsync(string chainId, string address)
    {
        return await _referralInviteProvider.GetInviteCountAsync(chainId, address);
    }
    
    [HttpGet("get-ad-hash")]
    public async Task<string> GetAdHashAsync(long timeStamp)
    {
        return await _userService.GetAdHashAsync(timeStamp);
    }
}