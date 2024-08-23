using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Ranking;
using TomorrowDAOServer.Ranking.Dto;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Ranking")]
[Route("api/app/ranking")]
public class RankingController
{
    private readonly IRankingAppService _rankingAppService;

    public RankingController(IRankingAppService rankingAppService)
    {
        _rankingAppService = rankingAppService;
    }
    
    [HttpGet("default-proposal")]
    [Authorize]
    public async Task<RankingDetailDto> GetDefaultRankingProposalAsync(string chainId)
    {
        return await _rankingAppService.GetDefaultRankingProposalAsync(chainId);
    }
    
    [HttpGet("list")]
    public async Task<PageResultDto<RankingListDto>> GetRankingProposalListAsync(GetRankingListInput input)
    {
        return await _rankingAppService.GetRankingProposalListAsync(input);
    }
    
    [HttpGet("detail")]
    public async Task<RankingDetailDto> GetRankingProposalDetailAsync(string chainId, string proposalId)
    {
        return await _rankingAppService.GetRankingProposalDetailAsync(chainId, proposalId, string.Empty);
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
}