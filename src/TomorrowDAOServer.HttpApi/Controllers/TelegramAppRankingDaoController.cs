using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("RankingDao")]
[Route("api/app/rankingdao")]
public class TelegramAppRankingDaoController : AbpController
{
    private readonly ILogger<TelegramAppRankingDaoController> _logger;

    public TelegramAppRankingDaoController(ILogger<TelegramAppRankingDaoController> logger)
    {
        _logger = logger;
    }
}