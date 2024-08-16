using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Spider;
using TomorrowDAOServer.Spider.Dto;
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
    private readonly IForumSpiderService _forumSpiderService;


    public ForumController(ILogger<TreasuryControllers> logger, IForumSpiderService forumSpiderService)
    {
        _logger = logger;
        _forumSpiderService = forumSpiderService;
    }

    [HttpPost("link-preview")]
    public async Task<LinkPreviewDto> LinkPreviewAsync(LinkPreviewInput input)
    {
        return await _forumSpiderService.LinkPreviewAsync(input);
    }
}