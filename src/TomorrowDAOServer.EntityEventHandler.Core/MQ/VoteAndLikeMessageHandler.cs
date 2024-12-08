using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Eto;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace TomorrowDAOServer.EntityEventHandler.Core.MQ;

public class VoteAndLikeMessageHandler : IDistributedEventHandler<VoteAndLikeMessageEto>, ITransientDependency
{
    private readonly ILogger<VoteAndLikeMessageHandler> _logger;
    private readonly IProposalProvider _proposalProvider;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly IRankingAppPointsProvider _appPointsProvider;
    private readonly ITelegramAppsProvider _telegramAppsProvider;

    public VoteAndLikeMessageHandler(ILogger<VoteAndLikeMessageHandler> logger, IProposalProvider proposalProvider,
        IRankingAppProvider rankingAppProvider, IRankingAppPointsProvider appPointsProvider,
        ITelegramAppsProvider telegramAppsProvider)
    {
        _logger = logger;
        _proposalProvider = proposalProvider;
        _rankingAppProvider = rankingAppProvider;
        _appPointsProvider = appPointsProvider;
        _telegramAppsProvider = telegramAppsProvider;
    }

    public async Task HandleEventAsync(VoteAndLikeMessageEto eventData)
    {
        Log.Information("[RankingAppPoints] process messages: {0}", JsonConvert.SerializeObject(eventData));
        if (eventData == null)
        {
            return;
        }

        var pointsType = eventData.PointsType;
        switch (pointsType)
        {
            case PointsType.InviteVote:
            case PointsType.BeInviteVote:
                await HandleReferralVoteAsync(eventData);
                break;
            case PointsType.Like:
            case PointsType.Vote:
            case PointsType.Open:
                await HandleDefaultAsync(eventData);
                break;
        }
    }

    private async Task HandleDefaultAsync(VoteAndLikeMessageEto eventData)
    {
        if (eventData.ProposalId.IsNullOrWhiteSpace() && eventData.Alias.IsNullOrWhiteSpace())
        {
            return;
        }

        try
        {
            if (!eventData.ProposalId.IsNullOrWhiteSpace())
            {
                var rankingAppIndex =
                    await _rankingAppProvider.GetByProposalIdAndAliasAsync(eventData.ChainId, eventData.ProposalId,
                        eventData.Alias);
                if (rankingAppIndex == null || rankingAppIndex.Id.IsNullOrWhiteSpace())
                {
                    Log.Error("[RankingAppPoints] ranking not found. proposalId={0},alias={1}", eventData.ProposalId,
                        eventData.Alias);
                    return;
                }
                
                eventData.DaoId = rankingAppIndex.DAOId;
                eventData.AppId = rankingAppIndex.AppId;
                eventData.Title = rankingAppIndex.Title;
            } else if (!eventData.Alias.IsNullOrWhiteSpace() && eventData.Title.IsNullOrWhiteSpace())
            {
                var telegramAppIndices = await _telegramAppsProvider.GetAllTelegramAppsAsync(new QueryTelegramAppsInput
                {
                    Aliases = new List<string>() {eventData.Alias},
                });
                if (telegramAppIndices.IsNullOrEmpty())
                {
                    Log.Error("[RankingAppPoints] app not found. proposalId={0},alias={1}", eventData.ProposalId,
                        eventData.Alias);
                    return;
                }

                var telegramAppIndex = telegramAppIndices.First();
                eventData.AppId = telegramAppIndex.Id;
                eventData.Title = telegramAppIndex.Title;
            }

            Log.Information("[RankingAppPoints] update app points. proposalId={0},alias={1}",
                eventData.ProposalId, eventData.Alias);
            await _appPointsProvider.AddOrUpdateAppPointsIndexAsync(eventData);
            Log.Information("[RankingAppPoints] update user points. proposalId={0},alias={1}",
                eventData.ProposalId, eventData.Alias);
            await _appPointsProvider.AddOrUpdateUserPointsIndexAsync(eventData);
        }
        catch (Exception e)
        {
            Log.Error(e, "[RankingAppPoints] process messages error. proposalId={0},alias={1}",
                eventData.ProposalId, eventData.Alias);
            throw;
        }
    }

    private async Task HandleReferralVoteAsync(VoteAndLikeMessageEto eventData)
    {
        Log.Information("[RankingAppPoints] updateUserReferralPoints. address={0},pointsType={1}",
            eventData.Address, eventData.PointsType.ToString());
        await _appPointsProvider.AddOrUpdateUserPointsIndexAsync(eventData);
    }
}