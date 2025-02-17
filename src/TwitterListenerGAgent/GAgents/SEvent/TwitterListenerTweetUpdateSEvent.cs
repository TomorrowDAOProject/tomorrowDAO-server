using Aevatar.Core.Abstractions;
using TwitterListenerGAgent.GAgents.Features.Dtos;

namespace TwitterListenerGAgent.GAgents.GAgents.SEvent;

[GenerateSerializer]
public class TwitterListenerTweetUpdateSEvent : TwitterListenerSEvent
{
    [Id(0)] public UserPostsResponseDto Tweets { get; set; }

    [Id(1)] public string SinceId { get; set; }
}