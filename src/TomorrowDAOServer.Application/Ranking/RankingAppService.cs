using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Ranking;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class RankingAppService : TomorrowDAOServerAppService, IRankingAppService
{
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IObjectMapper _objectMapper;

    public RankingAppService(IRankingAppProvider rankingAppProvider, ITelegramAppsProvider telegramAppsProvider, 
        IObjectMapper objectMapper)
    {
        _rankingAppProvider = rankingAppProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _objectMapper = objectMapper;
    }

    public async Task GenerateRankingApp(List<IndexerProposalDto> proposalList)
    {
        var toUpdate = new List<RankingAppIndex>();
        foreach (var proposal in proposalList)
        {
            var aliases = proposal.ProposalDescription.Replace("##GameRanking:", "").Trim()
                .Split(',').Select(alias => alias.Trim()).Distinct().ToList();
            var telegramApps = (await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
            {
                Aliases = aliases
            })).Item2;
            var rankingApps = _objectMapper.Map<List<TelegramAppIndex>, List<RankingAppIndex>>(telegramApps);
            foreach (var rankingApp in rankingApps)
            {
                rankingApp.OfProposal(proposal);
                rankingApp.Id = GuidHelper.GenerateGrainId(proposal.Id, rankingApp.AppId);
            }
            toUpdate.AddRange(rankingApps);
        }

        await _rankingAppProvider.BulkAddOrUpdateAsync(toUpdate);
    }
}