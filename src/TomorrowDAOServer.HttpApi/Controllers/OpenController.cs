using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Open;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Open")]
[Route("api/app/open")]
public class OpenController
{
    private readonly IOpenService _openService;

    public OpenController(IOpenService openService)
    {
        _openService = openService;
    }

    [HttpGet("task-status")]
    public async Task<TaskStatusResponse> GetTaskStatusAsync(string address, string proposalId)
    {
        return await _openService.GetTaskStatusAsync(address, proposalId);
    }
}