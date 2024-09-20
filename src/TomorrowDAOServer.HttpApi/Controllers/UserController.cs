using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}