using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("User")]
[Route("api/app/user")]
public class UserController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("user-source-report")]
    [Authorize]
    public async Task<UserSourceReportResultDto> UserSourceAsync(string chainId, string source)
    {
        return await _userService.UserSourceReportAsync(chainId, source);
    }
    
    [HttpGet("complete-task")]
    [Authorize]
    public async Task<bool> CompleteTaskAsync(CompleteTaskInput input)
    {
        return await _userService.CompleteTaskAsync(input);
    }
    
    [HttpGet("my-points")]
    [Authorize]
    public async Task<VoteHistoryPagedResultDto<MyPointsDto>> GetMyPointsAsync(GetMyPointsInput input)
    {
        return await _userService.GetMyPointsAsync(input);
    }
    
    [HttpGet("task-list")]
    [Authorize]
    public async Task<TaskListDto> GetTaskListAsync(string chainId)
    {
        return await _userService.GetTaskListAsync(chainId);
    }
}