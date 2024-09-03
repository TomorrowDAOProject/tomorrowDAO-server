using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Eto;
using TomorrowDAOServer.Ranking.Provider;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace TomorrowDAOServer.EntityEventHandler.Core.MQ;

public class VoteAndLikeMessageHandler : IDistributedEventHandler<VoteAndLikeMessageEto>, ITransientDependency
{
    private readonly ILogger<VoteAndLikeMessageHandler> _logger;
    private readonly IProposalProvider _proposalProvider;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly IRankingAppPointsProvider _appPointsProvider;

    public VoteAndLikeMessageHandler(ILogger<VoteAndLikeMessageHandler> logger, IProposalProvider proposalProvider,
        IRankingAppProvider rankingAppProvider, IRankingAppPointsProvider appPointsProvider)
    {
        _logger = logger;
        _proposalProvider = proposalProvider;
        _rankingAppProvider = rankingAppProvider;
        _appPointsProvider = appPointsProvider;
    }

    public async Task HandleEventAsync(VoteAndLikeMessageEto eventData)
    {
        _logger.LogInformation("[RankingAppPoints] process messages: {0}", JsonConvert.SerializeObject(eventData));
        if (eventData == null || eventData.ProposalId.IsNullOrWhiteSpace() || eventData.Alias.IsNullOrWhiteSpace())
        {
            return;
        }

        try
        {
            var rankingAppIndex =
                await _rankingAppProvider.GetByProposalIdAndAliasAsync(eventData.ChainId, eventData.ProposalId,
                    eventData.Alias);
            if (rankingAppIndex == null || rankingAppIndex.Id.IsNullOrWhiteSpace())
            {
                _logger.LogError("[RankingAppPoints] app not found. proposalId={0},alias={1}", eventData.ProposalId,
                    eventData.Alias);
                return;
            }

            eventData.DaoId = rankingAppIndex.DAOId;
            eventData.AppId = rankingAppIndex.AppId;
            eventData.Title = rankingAppIndex.Title;
            _logger.LogInformation("[RankingAppPoints] update app points. proposalId={0},alias={1}",
                eventData.ProposalId, eventData.Alias);
            await _appPointsProvider.AddOrUpdateAppPointsIndexAsync(eventData);
            _logger.LogInformation("[RankingAppPoints] update user points. proposalId={0},alias={1}",
                eventData.ProposalId, eventData.Alias);
            await _appPointsProvider.AddOrUpdateUserPointsIndexAsync(eventData);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[RankingAppPoints] process messages error. proposalId={0},alias={1}",
                eventData.ProposalId, eventData.Alias);
            throw;
        }
    }
}