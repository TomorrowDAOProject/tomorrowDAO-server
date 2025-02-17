using Aevatar.Core;
using Aevatar.Core.Abstractions;
using AevatarTemplate.GAgents.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TwitterListenerGAgent.GAgents.Features.Common;
using TwitterListenerGAgent.GAgents.Features.Grains;
using TwitterListenerGAgent.GAgents.GAgents.SEvent;
using TwitterListenerGAgent.GAgents.GEvents;

namespace TwitterListenerGAgent.GAgents.GAgents;

public class TwitterListenerGAgent : GAgentBase<TwitterListenerGAgentState, TwitterListenerSEvent, EventBase,
    TwitterListenerOptions>
{
    private readonly ILogger<TwitterListenerGAgent> _logger;

    public TwitterListenerGAgent(ILogger<TwitterListenerGAgent> logger) : base(logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Monitor KOL tweets");
    }


    public override async Task InitializeAsync(TwitterListenerOptions initializationEvent)
    {
        _logger.LogInformation("Initialize TwitterListenerGAgent，{0}",
            JsonConvert.SerializeObject(initializationEvent));

        var twitterListenerGrain = GrainFactory.GetGrain<ITwitterListenerGrain>(Guid.NewGuid());
        var lookupByUserNameResponse = await twitterListenerGrain.UserLookupByUserNamesAsync(initializationEvent.BearerToken, initializationEvent.KOL);
        if (lookupByUserNameResponse.Data.IsNullOrEmpty())
        {
            _logger.LogError("Initialize TwitterListenerGAgent fail. User not exist.{0}", initializationEvent.KOL);
            throw new SystemException("User not exist");
        }

        RaiseEvent(new TwitterListenerUpdateKOLSEvent
        {
            Id = default,
            Ctime = DateTime.UtcNow,
            KOL = lookupByUserNameResponse.Data.First(),
            BearerToken = initializationEvent.BearerToken,
            AccessToken = initializationEvent.AccessToken,
            AccessTokenSecret = initializationEvent.AccessTokenSecret
        });
        
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(PullTweetEvent @event)
    {
        _logger.LogInformation("[PullTweet] start GrainId: {0}", this.GetPrimaryKey().ToString());

        try
        {
            var twitterListenerGrain = GrainFactory.GetGrain<ITwitterListenerGrain>(Guid.NewGuid());
            var twitterResponseDto = await twitterListenerGrain.PullTweetAsync(State.KOL, State.BearerToken, State.SinceId);
            
            _logger.LogInformation("[PullTweet] number of tweets {0}",
                twitterResponseDto.Data.IsNullOrEmpty() ? 0 : twitterResponseDto.Data.Count);
            
            if (!twitterResponseDto.Data.IsNullOrEmpty())
            {
                RaiseEvent(new TwitterListenerTweetUpdateSEvent
                {
                    Tweets = twitterResponseDto,
                    SinceId = twitterResponseDto.Data.Last().Id
                });
                await ConfirmEvents();
            }

            await PublishAsync(new DataUpdateEvent
            {
                DateType = DataType.Tweet
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PullTweet] Exception handling for the PullTweetEvent");
        }
    }
}