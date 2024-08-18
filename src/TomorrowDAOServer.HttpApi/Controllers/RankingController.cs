using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
    
    [HttpGet("default-ranking")]
    public async Task<RankingResultDto> GetDefaultProposalAsync(string chainId)
    {
        return await _rankingAppService.GetDefaultProposalAsync(chainId);
    }
}