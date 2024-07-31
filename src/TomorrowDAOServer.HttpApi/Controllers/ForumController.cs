using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Forum.Dto;
using TomorrowDAOServer.Treasury;
using TomorrowDAOServer.Treasury.Dto;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Forum")]
[Route("api/app/forum")]
public class ForumController : AbpController
{
    private readonly ILogger<TreasuryControllers> _logger;
    private readonly ITreasuryAssetsService _treasuryAssetsService;

    public ForumController(ILogger<TreasuryControllers> logger)
    {
        _logger = logger;
    }

    [HttpPost("link-preview")]
    public async Task<LinkPreviewDto> LinkPreview(LinkPreviewInput input)
    {
        //return await _treasuryAssetsService.GetTreasuryAssetsAsync(input);
        return null;
    }
}