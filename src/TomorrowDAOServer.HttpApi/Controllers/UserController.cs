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
    private readonly IUserAppService _userAppService;

    public UserController(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    [HttpGet("user-source-report")]
    [Authorize]
    public async Task<UserSourceReportResultDto> UserSourceAsync(string chainId, long source)
    {
        return await _userAppService.UserSourceReportAsync(chainId, source);
    }
}