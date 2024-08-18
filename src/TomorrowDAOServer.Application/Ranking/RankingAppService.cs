using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Dto;
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
    private readonly IProposalProvider _proposalProvider;

    public RankingAppService(IRankingAppProvider rankingAppProvider, ITelegramAppsProvider telegramAppsProvider, 
        IObjectMapper objectMapper, IProposalProvider proposalProvider)
    {
        _rankingAppProvider = rankingAppProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _objectMapper = objectMapper;
        _proposalProvider = proposalProvider;
    }

    public async Task GenerateRankingApp(List<IndexerProposal> proposalList)
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
                _objectMapper.Map<IndexerProposal, RankingAppIndex>(proposal);
                rankingApp.Id = GuidHelper.GenerateGrainId(proposal.ChainId, proposal.DAOId, proposal.Id, rankingApp.AppId);
            }
            toUpdate.AddRange(rankingApps);
        }

        await _rankingAppProvider.BulkAddOrUpdateAsync(toUpdate);
    }

    public async Task<RankingResultDto> GetDefaultProposalAsync(string chainId)
    {
        //todo supplement
        return new RankingResultDto();
    }
}