using TwitterListenerGAgent.GAgents.Features.Dtos;

namespace TwitterListenerGAgent.GAgents.Features.Grains;

public interface ITwitterListenerGrain : IGrainWithGuidKey
{
    Task<UserPostsResponseDto> PullTweetAsync(KOLInfoDto KOL, string bearerToken, string sinceId);
    Task<LookupByUserNameResponse> UserLookupByUserNamesAsync(string bearerToken, string kolName);
}