using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Ranking;
using TomorrowDAOServer.Ranking.Dto;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Ranking")]
[Route("api/app/ranking")]
public class RankingController : AbpController
{
    private readonly IRankingAppService _rankingAppService;
    private readonly IVotigramRevampDataMigrator _votigramRevampDataMigrator;

    public RankingController(IRankingAppService rankingAppService,
        IVotigramRevampDataMigrator votigramRevampDataMigrator)
    {
        _rankingAppService = rankingAppService;
        _votigramRevampDataMigrator = votigramRevampDataMigrator;
    }

    [HttpGet("default-proposal")]
    [Authorize]
    public async Task<RankingDetailDto> GetDefaultRankingProposalAsync(string chainId)
    {
        return await _rankingAppService.GetDefaultRankingProposalAsync(chainId);
    }

    [HttpGet("list")]
    [Authorize]
    public async Task<RankingListPageResultDto<RankingListDto>> GetRankingProposalListAsync(GetRankingListInput input)
    {
        return await _rankingAppService.GetRankingProposalListAsync(input);
    }

    [HttpGet("detail")]
    [Authorize]
    public async Task<RankingDetailDto> GetRankingProposalDetailAsync(string chainId, string proposalId)
    {
        return await _rankingAppService.GetRankingProposalDetailAsync(chainId, proposalId);
    }

    [HttpPost("vote")]
    [Authorize]
    public async Task<RankingVoteResponse> VoteAsync(RankingVoteInput input)
    {
        return await _rankingAppService.VoteAsync(input);
    }

    [HttpPost("vote/status")]
    [Authorize]
    public async Task<RankingVoteRecord> GetVoteStatusAsync(GetVoteStatusInput input)
    {
        return await _rankingAppService.GetVoteStatusAsync(input);
    }

    [HttpGet("move-history-data")]
    [Authorize]
    public async Task HistoryDataAsync(string chainId, string type, string key, string value)
    {
        await _rankingAppService.MoveHistoryDataAsync(chainId, type, key, value);
    }

    [HttpPost("like")]
    [Authorize]
    public async Task<RankingAppLikeResultDto> LikeAsync(RankingAppLikeInput input)
    {
        return await _rankingAppService.LikeAsync(input);
    }

    [HttpGet("activity-result")]
    [Authorize]
    public async Task<RankingActivityResultDto> GetRankingActivityResultAsync(string chainId, string proposalId,
        int count)
    {
        return await _rankingAppService.GetRankingActivityResultAsync(chainId, proposalId, count);
    }

    [HttpGet("banner-info")]
    [Authorize]
    public async Task<RankingBannerInfo> GetBannerInfoAsync(string chainId)
    {
        return await _rankingAppService.GetBannerInfoAsync(chainId);
    }

    [HttpGet("poll-list")]
    [Authorize]
    public async Task<RankingListPageResultDto<RankingListDto>> GetPollListAsync(GetPollListInput input)
    {
        return await _rankingAppService.GetPollListAsync(input);
    }

    [HttpGet("votigram-revamp-data-migrator")]
    [Authorize]
    public async Task VotigramRevampDataMigratorAsync(string chainId, bool dealDuplicateApp, bool dealRankingApp,
        bool dealTelegramApp, bool dealRankingAppPointIndex, bool dealRankingAppUserPointsIndex, bool dealUserTaskIndex,
        bool dealUserTotalPoints)
    {
        await _votigramRevampDataMigrator.MigrateHistoricalDataAsync(chainId, dealDuplicateApp, dealRankingApp,
            dealTelegramApp, dealRankingAppPointIndex, dealRankingAppUserPointsIndex, dealUserTaskIndex,
            dealUserTotalPoints);
    }
}