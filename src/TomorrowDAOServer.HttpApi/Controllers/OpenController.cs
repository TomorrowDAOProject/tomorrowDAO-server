using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Open;
using TomorrowDAOServer.Open.Dto;
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

    [HttpGet("micro3-task-status")]
    public async Task<TaskStatusResponse> GetMicro3TaskStatusAsync(string address)
    {
        return await _openService.GetMicro3TaskStatusAsync(address);
    }
    
    [HttpGet("task-status")]
    public async Task<bool> GetFoxCoinTaskStatusAsync(string id, string type)
    {
        return await _openService.GetFoxCoinTaskStatusAsync(id, type);
    }
    
    [HttpGet("galxe-task-status")]
    public async Task<int> GetGalxeTaskStatusAsync(string address)
    {
        return await _openService.GetGalxeTaskStatusAsync(address);
    }
    
}