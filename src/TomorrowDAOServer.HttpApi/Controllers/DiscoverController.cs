using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Discover;
using TomorrowDAOServer.Discover.Dto;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Discover")]
[Route("api/app/discover")]
public class DiscoverController
{
    private readonly IDiscoverService _discoverService;

    public DiscoverController(IDiscoverService discoverService)
    {
        _discoverService = discoverService;
    }
    
    [HttpGet("view")]
    [Authorize]
    public async Task<bool> DiscoverViewedAsync(string chainId)
    {
        return await _discoverService.DiscoverViewedAsync(chainId);
    }
    
    [HttpGet("choose")]
    [Authorize]
    public async Task<bool> DiscoverChooseAsync(string chainId, List<string> choices)
    {
        return await _discoverService.DiscoverChooseAsync(chainId, choices);
    }
    
    [HttpGet("app-list")]
    [Authorize]
    public async Task<List<DiscoverAppDto>> GetDiscoverAppListAsync(GetDiscoverAppListInput input)
    {
        return await _discoverService.GetDiscoverAppListAsync(input);
    }
}