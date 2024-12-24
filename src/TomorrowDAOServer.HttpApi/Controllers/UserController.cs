using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Telegram.Dto;
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
    
    [HttpPost("view-ad")]
    [Authorize]
    public async Task<long> ViewAdAsync(ViewAdInput input)
    {
        return await _userService.ViewAdAsync(input);
    }
    
    [HttpPost("save-tg-info")]
    [Authorize]
    public async Task<bool> SaveTgInfoAsync(SaveTgInfoInput input)
    {
        return await _userService.SaveTgInfoAsync(input);
    }

    [HttpGet("login-points/status")]
    [Authorize]
    public async Task<LoginPointsStatusDto> GetLoginPointsStatusAsync(GetLoginPointsStatusInput input)
    {
        return await _userService.GetLoginPointsStatusAsync(input);
    }

    [HttpPost("login-points/collect")]
    [Authorize]
    public async Task<LoginPointsStatusDto> CollectLoginPointsAsync(CollectLoginPointsInput input)
    {
        return await _userService.CollectLoginPointsAsync(input);
    }

    [HttpGet("homepage")]
    [Authorize]
    public async Task<HomePageResultDto> GetHomePageAsync(GetHomePageInput input)
    {
        return await _userService.GetHomePageAsync(input);
    }
    
    [HttpGet("homepage/made-for-you")]
    [Authorize]
    public async Task<PageResultDto<AppDetailDto>> GetMadeForYouAsync(GetMadeForYouInput input)
    {
        return await _userService.GetMadeForYouAsync(input);
    }

    [HttpPost("open-app")]
    [Authorize]
    public async Task<bool> OpenAppAsync(OpenAppInput input)
    {
        return await _userService.OpenAppAsync(input);
    }
}