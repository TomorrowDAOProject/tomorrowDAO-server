using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace TomorrowDAOServer.Ranking;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class RankingAppService : TomorrowDAOServerAppService, IRankingAppService
{
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IProposalProvider _proposalProvider;
    private readonly IUserProvider _userProvider;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;

    public RankingAppService(IRankingAppProvider rankingAppProvider, ITelegramAppsProvider telegramAppsProvider, 
        IObjectMapper objectMapper, IProposalProvider proposalProvider, IUserProvider userProvider, 
        IOptionsMonitor<RankingOptions> rankingOptions)
    {
        _rankingAppProvider = rankingAppProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _objectMapper = objectMapper;
        _proposalProvider = proposalProvider;
        _userProvider = userProvider;
        _rankingOptions = rankingOptions;
    }

    public async Task GenerateRankingApp(List<IndexerProposal> proposalList)
    {
        var toUpdate = new List<RankingAppIndex>();
        var descriptionBegin = _rankingOptions.CurrentValue.DescriptionBegin;
        foreach (var proposal in proposalList)
        {
            var aliases = proposal.ProposalDescription.Replace(descriptionBegin, CommonConstant.EmptyString)
                .Trim().Split(CommonConstant.Comma).Select(alias => alias.Trim()).Distinct().ToList();
            var telegramApps = (await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
            {
                Aliases = aliases
            })).Item2;
            var rankingApps = _objectMapper.Map<List<TelegramAppIndex>, List<RankingAppIndex>>(telegramApps);
            foreach (var rankingApp in rankingApps)
            {
                _objectMapper.Map(proposal, rankingApp);
                rankingApp.Id = GuidHelper.GenerateGrainId(proposal.ChainId, proposal.DAOId, proposal.Id, rankingApp.AppId);
            }
            toUpdate.AddRange(rankingApps);
        }

        await _rankingAppProvider.BulkAddOrUpdateAsync(toUpdate);
    }

    public async Task<RankingDetailDto> GetDefaultRankingProposalAsync(string chainId)
    {
        var defaultProposal = await _proposalProvider.GetDefaultProposalAsync(chainId);
        if (defaultProposal == null)
        {
            return new RankingDetailDto();
        }

        return await GetRankingProposalDetailAsync(chainId, defaultProposal.ProposalId);
    }

    public async Task<PageResultDto<RankingListDto>> GetRankingProposalListAsync(GetRankingListInput input)
    {
        var result = await _proposalProvider.GetRankingProposalListAsync(input);
        // todo vote related logic
        return new PageResultDto<RankingListDto>
        {
            TotalCount = result.Item1,
            Data = ObjectMapper.Map<List<ProposalIndex>, List<RankingListDto>>(result.Item2)
        };
    }

    public async Task<RankingDetailDto> GetRankingProposalDetailAsync(string chainId, string proposalId)
    {
        var userAddress = string.Empty;
        try
        {
            userAddress = await _userProvider.GetUserAddressAsync(CurrentUser.GetId(), chainId);
        }
        catch (Exception)
        {
            // ignored
        }

        return await GetRankingProposalDetailAsync(userAddress, chainId, proposalId);
    }

    private async Task<RankingDetailDto> GetRankingProposalDetailAsync(string userAddress, string chainId, string proposalId)
    {
        var rankingAppList = await _rankingAppProvider.GetByProposalIdAsync(chainId, proposalId);
        if (rankingAppList.IsNullOrEmpty())
        {
            return new RankingDetailDto();
        }

        // todo vote related logic
        var rankingApp = rankingAppList[0];
        return new RankingDetailDto
        {
            StartTime = rankingApp.ActiveStartTime,
            EndTime = rankingApp.ActiveEndTime,
            CanVoteAmount = 0,
            TotalVoteAmount = 0,
            RankingList = ObjectMapper.Map<List<RankingAppIndex>, List<RankingAppDetailDto>>(rankingAppList)
        };
    }
}