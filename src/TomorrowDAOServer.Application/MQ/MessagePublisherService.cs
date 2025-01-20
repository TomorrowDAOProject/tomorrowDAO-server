using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Eto;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace TomorrowDAOServer.MQ;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class MessagePublisherService : TomorrowDAOServerAppService, IMessagePublisherService
{
    private readonly ILogger<MessagePublisherService> _logger;
    private readonly IDistributedEventBus _distributedEventBus;

    public MessagePublisherService(ILogger<MessagePublisherService> logger, IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _distributedEventBus = distributedEventBus;
    }

    [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default,
        Message = "SendLikeMessageAsync error",
        LogTargets = new []{"chainId", "proposalId", "address", "likeList"})]
    public virtual async Task SendLikeMessageAsync(string chainId, string proposalId, string address,
        List<RankingAppLikeDetailDto> likeList, string userId = "", Dictionary<string, long> addedAliasDic = null)
    {
        Log.Information("SendLikeMessageAsync, chainId={0}, proposalId={1}, address={2}, like={3}", chainId,
            proposalId, address, JsonConvert.SerializeObject(likeList));

        if (likeList.IsNullOrEmpty())
        {
            return;
        }

        foreach (var likeDetail in likeList)
        {
            var added = addedAliasDic != null && addedAliasDic.TryGetValue(likeDetail.Alias, out _);
            await _distributedEventBus.PublishAsync(new VoteAndLikeMessageEto
            {
                ChainId = chainId,
                DaoId = null,
                ProposalId = proposalId,
                AppId = null,
                Alias = likeDetail.Alias,
                Title = null,
                Address = address,
                Amount = likeDetail.LikeAmount,
                PointsType = PointsType.Like,
                UserId = userId,
                Added = added
            });
        }
    }

    [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default,
        Message = "SendVoteMessageAsync error",
        LogTargets = new []{"chainId", "proposalId", "address", "appAlias", "amount"})]
    public virtual async Task SendVoteMessageAsync(string chainId, string proposalId, string address, string appAlias,
        long amount, bool dailyVote = false)
    {
        Log.Information("SendVoteMessageAsync, chainId={0}, proposalId={1}, address={2}, alias={3}, amount={4}",
            chainId, proposalId, address, appAlias, amount);
        await _distributedEventBus.PublishAsync(new VoteAndLikeMessageEto
        {
            ChainId = chainId,
            DaoId = null,
            ProposalId = proposalId,
            AppId = null,
            Alias = appAlias,
            Title = null,
            Address = address,
            Amount = amount,
            PointsType = PointsType.Vote,
            DailyFirstVote = dailyVote
        });
    }
    
    [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default,
        Message = "SendReferralFirstVoteMessageAsync error",
        LogTargets = new []{"chainId", "inviter", "invitee"})]
    public virtual async Task SendReferralFirstVoteMessageAsync(string chainId, string inviter, string invitee)
    {
        Log.Information("SendReferralFirstVoteMessageAsync, chainId={0}, inviter={1}, invitee={2}", 
            chainId, inviter, invitee);

        await _distributedEventBus.PublishAsync(new VoteAndLikeMessageEto
        {
            ChainId = chainId,
            DaoId = string.Empty,
            ProposalId = string.Empty,
            AppId = string.Empty,
            Alias = string.Empty,
            Title = string.Empty,
            Address = inviter,
            Amount = 1,
            PointsType = PointsType.InviteVote
        });
            
        await _distributedEventBus.PublishAsync(new VoteAndLikeMessageEto
        {
            ChainId = chainId,
            DaoId = string.Empty,
            ProposalId = string.Empty,
            AppId = string.Empty,
            Alias = string.Empty,
            Title = string.Empty,
            Address = invitee,
            Amount = 1,
            PointsType = PointsType.BeInviteVote
        });
    }

    [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default,
        Message = "SendOpenMessageAsync error",
        LogTargets = new []{"chainId", "address", "userId", "appAlias", "count"})]
    public async Task SendOpenMessageAsync(string chainId, string address, string userId, string appAlias, long count)
    {
        Log.Information("SendOpenMessageAsync, chainId={0}, address={1}, userId={2}, appAlias={3}, count={4}", 
            chainId, address, userId, appAlias, count);

        await _distributedEventBus.PublishAsync(new VoteAndLikeMessageEto
        {
            ChainId = chainId,
            DaoId = string.Empty,
            ProposalId = string.Empty,
            AppId = string.Empty,
            Alias = appAlias,
            Title = string.Empty,
            Address = address,
            Amount = 1,
            PointsType = PointsType.Open,
            UserId = userId
        });
    }
}