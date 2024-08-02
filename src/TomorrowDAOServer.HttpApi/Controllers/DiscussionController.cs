using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Discussion;
using TomorrowDAOServer.Discussion.Dto;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Discussion")]
[Route("api/app/discussion")]
public class DiscussionController
{
    private readonly IDiscussionService _discussionService;

    public DiscussionController(IDiscussionService discussionService)
    {
        _discussionService = discussionService;
    }
    
    [HttpPost("new-comment")]
    [Authorize]
    public async Task<NewCommentResultDto> NewCommentAsync(NewCommentInput input)
    {
        return await _discussionService.NewCommentAsync(input);
    }
    
    [HttpGet("comment-list")]
    public async Task<CommentListPageResultDto> GetCommentListAsync(GetCommentListInput input)
    {
        return await _discussionService.GetCommentListAsync(input);
    }
    
    [HttpGet("comment-building")]
    public async Task<CommentBuildingDto> GetCommentBuildingAsync(GetCommentBuildingInput input)
    {
        return await _discussionService.GetCommentBuildingAsync(input);
    }
}