using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Forum;
using TomorrowDAOServer.Forum.Dto;
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
    private readonly IForumService _forumService;


    public ForumController(ILogger<TreasuryControllers> logger, IForumService forumService)
    {
        _logger = logger;
        _forumService = forumService;
    }

    [HttpPost("link-preview")]
    public async Task<LinkPreviewDto> LinkPreviewAsync(LinkPreviewInput input)
    {
        return await _forumService.LinkPreviewAsync(input);
    }
}