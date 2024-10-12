using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

    public async Task SendLikeMessageAsync(string chainId, string proposalId, string address,
        List<RankingAppLikeDetailDto> likeList)
    {
        _logger.LogInformation("SendLikeMessageAsync, chainId={0}, proposalId={1}, address={2}, like={3}", chainId,
            proposalId, address, JsonConvert.SerializeObject(likeList));

        try
        {
            if (likeList.IsNullOrEmpty())
            {
                return;
            }

            foreach (var likeDetail in likeList)
            {
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
                    PointsType = PointsType.Like
                });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SendLikeMessageAsync error: chainId={0}, proposalId={1}, address={2}, like={3}",
                chainId, proposalId, address, JsonConvert.SerializeObject(likeList));
        }
    }

    public async Task SendVoteMessageAsync(string chainId, string proposalId, string address, string appAlias,
        long amount)
    {
        _logger.LogInformation("SendVoteMessageAsync, chainId={0}, proposalId={1}, address={2}, alias={3}, amount={4}",
            chainId, proposalId, address, appAlias, amount);

        try
        {
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
                PointsType = PointsType.Vote
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "SendVoteMessageAsync error: chainId={0}, proposalId={1}, address={2}, alias={3}, amount={4}",
                chainId, proposalId, address, appAlias, amount);
        }
    }
    
    public async Task SendReferralFirstVoteMessageAsync(string chainId, string inviter, string invitee)
    {
        _logger.LogInformation("SendReferralFirstVoteMessageAsync, chainId={0}, inviter={1}, invitee={2}", 
            chainId, inviter, invitee);

        try
        {
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
        catch (Exception e)
        {
            _logger.LogError(e,
                "SendReferralFirstVoteMessageAsyncException, chainId={0}, inviter={1}, invitee={2}",
                chainId, inviter, invitee);
        }
    }
}