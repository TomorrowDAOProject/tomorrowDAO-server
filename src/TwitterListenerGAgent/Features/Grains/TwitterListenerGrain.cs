using Microsoft.Extensions.Logging;
using TwitterListenerGAgent.GAgents.Features.Dtos;
using TwitterListenerGAgent.GAgents.Features.Provider;

namespace TwitterListenerGAgent.GAgents.Features.Grains;

public class TwitterListenerGrain : Grain, ITwitterListenerGrain
{
    private ILogger<TwitterListenerGrain> _logger;
    private ITwitterListenerProvider _twitterListenerProvider;

    public TwitterListenerGrain(ILogger<TwitterListenerGrain> logger, ITwitterListenerProvider twitterListenerProvider)
    {
        _logger = logger;
        _twitterListenerProvider = twitterListenerProvider;
    }

    public async Task<UserPostsResponseDto> PullTweetAsync(KOLInfoDto KOL, string bearerToken, string sinceId)
    {
        var responseDto = await _twitterListenerProvider.PullUserLatestTweetsAsync(KOL.Id, bearerToken, sinceId);
        return responseDto;
    }

    public async Task<LookupByUserNameResponse> UserLookupByUserNamesAsync(string bearerToken, string kolName)
    {
        return await _twitterListenerProvider.UserLookupByUserNamesAsync(bearerToken, kolName);
    }
}