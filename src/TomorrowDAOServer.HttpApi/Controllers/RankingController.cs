using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Ranking;
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
}