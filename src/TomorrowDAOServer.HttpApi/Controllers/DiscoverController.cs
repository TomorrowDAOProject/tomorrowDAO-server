using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Common.Dtos;
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
    
    [HttpPost("choose")]
    [Authorize]
    public async Task<bool> DiscoverChooseAsync(GetDiscoverChooseInput input)
    {
        return await _discoverService.DiscoverChooseAsync(input.ChainId, input.Choices);
    }
    
    [HttpPost("app-list")]
    [Authorize]
    public async Task<PageResultDto<DiscoverAppDto>> GetDiscoverAppListAsync(GetDiscoverAppListInput input)
    {
        return await _discoverService.GetDiscoverAppListAsync(input);
    }
}