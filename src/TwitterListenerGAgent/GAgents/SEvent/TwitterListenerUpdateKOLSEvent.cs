using Aevatar.Core.Abstractions;
using TwitterListenerGAgent.GAgents.Features.Dtos;

namespace TwitterListenerGAgent.GAgents.GAgents.SEvent;

[GenerateSerializer]
public class TwitterListenerUpdateKOLSEvent : TwitterListenerSEvent
{
    [Id(0)] public KOLInfoDto KOL { get; set; }
    [Id(1)] public string BearerToken { get; set; }
    [Id(2)] public string AccessToken { get; set; }
    [Id(3)] public string AccessTokenSecret { get; set; }
}