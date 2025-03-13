using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Entities;
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
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;

    public VoteAndLikeMessageHandler(ILogger<VoteAndLikeMessageHandler> logger, IProposalProvider proposalProvider,
        IRankingAppProvider rankingAppProvider, IRankingAppPointsProvider appPointsProvider,
        ITelegramAppsProvider telegramAppsProvider, IRankingAppPointsCalcProvider rankingAppPointsCalcProvider)
    {
        _logger = logger;
        _proposalProvider = proposalProvider;
        _rankingAppProvider = rankingAppProvider;
        _appPointsProvider = appPointsProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
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
            case PointsType.Share:
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
            RankingAppIndex rankingAppIndex = null;
            TelegramAppIndex telegramAppIndex = null;
            if (!eventData.ProposalId.IsNullOrWhiteSpace())
            {
                rankingAppIndex =
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
            }

            if (!eventData.Alias.IsNullOrWhiteSpace() && eventData.Title.IsNullOrWhiteSpace())
            {
                var telegramAppIndices = await _telegramAppsProvider.GetAllTelegramAppsAsync(new QueryTelegramAppsInput
                {
                    Aliases = new List<string>() { eventData.Alias },
                });
                if (telegramAppIndices.IsNullOrEmpty())
                {
                    Log.Error("[RankingAppPoints] app not found. proposalId={0},alias={1}", eventData.ProposalId,
                        eventData.Alias);
                    return;
                }

                telegramAppIndex = telegramAppIndices.First();
                eventData.AppId = telegramAppIndex.Id;
                eventData.Title = telegramAppIndex.Title;
            }

            _logger.LogInformation("[RankingAppPoints] update app points. proposalId={0},alias={1}",
                eventData.ProposalId, eventData.Alias);
            await _appPointsProvider.AddOrUpdateAppPointsIndexAsync(eventData);
            _logger.LogInformation("[RankingAppPoints] update user points. proposalId={0},alias={1}",
                eventData.ProposalId, eventData.Alias);
            await _appPointsProvider.AddOrUpdateUserPointsIndexAsync(eventData);
            _logger.LogInformation(
                "[RankingAppPoints] update Telegrap And Ranking points, proposalId={0},alias={1}, pointsType={2}",
                eventData.ProposalId, eventData.Alias, eventData.PointsType.ToString());
            await UpdateTelegramAndRankingAppPointsAsync(eventData, rankingAppIndex, telegramAppIndex);
        }
        catch (Exception e)
        {
            Log.Error(e, "[RankingAppPoints] process messages error. proposalId={0},alias={1}",
                eventData.ProposalId, eventData.Alias);
            throw;
        }
    }

    private async Task UpdateTelegramAndRankingAppPointsAsync(VoteAndLikeMessageEto eventData,
        RankingAppIndex rankingAppIndex, TelegramAppIndex telegramAppIndex)
    {
        if (eventData.PointsType is PointsType.Vote or PointsType.Like)
        {
            long points = 0;
            long voteCount = 0;
            long likeCount = 0;
            long openCount = 0;
            if (eventData.PointsType == PointsType.Vote)
            {
                voteCount = eventData.Amount;
                points = _rankingAppPointsCalcProvider.CalculatePointsFromVotes(eventData.Amount);
            }
            else if (eventData.PointsType == PointsType.Like)
            {
                likeCount = eventData.Amount;
                points = _rankingAppPointsCalcProvider.CalculatePointsFromLikes(eventData.Amount);
            }
            else if (eventData.PointsType == PointsType.Open)
            {
                openCount = eventData.Amount;
            }

            if (points > 0 && rankingAppIndex != null)
            {
                rankingAppIndex.TotalPoints += points;
                rankingAppIndex.TotalVotes += voteCount;
                rankingAppIndex.TotalLikes += likeCount;
                await _rankingAppProvider.BulkAddOrUpdateAsync(new List<RankingAppIndex>() { rankingAppIndex });
                _logger.LogInformation(
                    "[RankingAppPoints] update ranking points, proposalId={0},alias={1}, TotalPoints={2}, TotalVotes={3}, TotalLikes={4},",
                    eventData.ProposalId, eventData.Alias, rankingAppIndex.TotalPoints, rankingAppIndex.TotalVotes,
                    rankingAppIndex.TotalLikes);
            }

            if ((points > 0 || openCount > 0) && telegramAppIndex != null)
            {
                telegramAppIndex.TotalPoints += points;
                telegramAppIndex.TotalVotes += voteCount;
                telegramAppIndex.TotalLikes += likeCount;
                telegramAppIndex.TotalOpenTimes += openCount;
                await _telegramAppsProvider.AddOrUpdateAsync(telegramAppIndex);
                _logger.LogInformation(
                    "[RankingAppPoints] update telegram points, alias={0}, TotalPoints={1}, TotalVotes={2}, TotalLikes={3}, TotalOpenTimes={4}",
                    eventData.Alias, telegramAppIndex.TotalPoints, telegramAppIndex.TotalVotes,
                    telegramAppIndex.TotalLikes, telegramAppIndex.TotalOpenTimes);
            }
        }
    }

    private async Task HandleReferralVoteAsync(VoteAndLikeMessageEto eventData)
    {
        Log.Information("[RankingAppPoints] updateUserReferralPoints. address={0},pointsType={1}",
            eventData.Address, eventData.PointsType.ToString());
        await _appPointsProvider.AddOrUpdateUserPointsIndexAsync(eventData);
    }
}