using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TomorrowDAOServer.Enums;
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
        IDictionary<string, long> appAmounts)
    {
        _logger.LogInformation("SendLikeMessageAsync, chainId={0}, proposalId={1}, address={2}, like={3}", chainId,
            proposalId, address, JsonConvert.SerializeObject(appAmounts));

        if (appAmounts.IsNullOrEmpty())
        {
            return;
        }

        foreach (var appAmount in appAmounts)
        {
            await _distributedEventBus.PublishAsync(new VoteAndLikeMessageEto
            {
                ChainId = chainId,
                DaoId = null,
                ProposalId = proposalId,
                AppId = null,
                Alias = appAmount.Key,
                Title = null,
                Amount = appAmount.Value,
                PointsType = PointsType.VotePoints
            });
        }
    }

    public async Task SendVoteMessageAsync(string chainId, string proposalId, string address, string appAlias, long amount)
    {
        _logger.LogInformation("SendLikeMessageAsync, chainId={0}, proposalId={1}, address={2}, alias={3}, amount={4}",
            chainId, proposalId, address, appAlias, amount);
        
        await _distributedEventBus.PublishAsync(new VoteAndLikeMessageEto
        {
            ChainId = chainId,
            DaoId = null,
            ProposalId = proposalId,
            AppId = null,
            Alias = appAlias,
            Title = null,
            Amount = amount,
            PointsType = PointsType.VotePoints
        });
    }
}