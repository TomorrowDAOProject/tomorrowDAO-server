using Aevatar.Core.Abstractions;
using TwitterListenerGAgent.GAgents.Features.Dtos;
using TwitterListenerGAgent.GAgents.GAgents.SEvent;

namespace TwitterListenerGAgent.GAgents.GAgents;

[GenerateSerializer]
public class TwitterListenerGAgentState : StateBase
{
    [Id(0)] public KOLInfoDto KOL { get; set; }
    
    [Id(1)] public string BearerToken { get; set; }
    [Id(2)] public string AccessToken { get; set; }
    [Id(3)] public string AccessTokenSecret { get; set; }
    
    [Id(4)] public string SinceId { get; set; }
    

    public void Apply(TwitterListenerUpdateKOLSEvent updateKOLEvent)
    {
        KOL = updateKOLEvent.KOL;
        BearerToken = updateKOLEvent.BearerToken;
        AccessToken = updateKOLEvent.AccessToken;
        AccessTokenSecret = updateKOLEvent.AccessTokenSecret;
    }

    public void Apply(TwitterListenerTweetUpdateSEvent tweetUpdateSEvent)
    {
        SinceId = tweetUpdateSEvent.SinceId;
    }
}