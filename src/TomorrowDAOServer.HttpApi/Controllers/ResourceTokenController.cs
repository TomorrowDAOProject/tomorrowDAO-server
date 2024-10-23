using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.ResourceToken;
using TomorrowDAOServer.ResourceToken.Dtos;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Dao")]
[Route("api/app/resource")]
public class ResourceTokenController
{
    private readonly IResourceTokenService _resourceTokenService;

    public ResourceTokenController(IResourceTokenService resourceTokenService)
    {
        _resourceTokenService = resourceTokenService;
    }
    
    [HttpGet("realtime-records")]
    public async Task<RealtimeRecordsDto> GetRealtimeRecordsAsync(int limit, string type)
    {
        return await _resourceTokenService.GetRealtimeRecordsAsync(limit, type);
    }
    
    [HttpGet("turnover")]
    public async Task<TurnoverDto> GetTurnoverAsync(GetTurnoverInput input)
    {
        return await _resourceTokenService.GetTurnoverAsync(input);
    }
    
    [HttpGet("records")]
    public async Task<RecordPageDto> GetRecordsAsync(GetRecordsInput input)
    {
        return await _resourceTokenService.GetRecordsAsync(input);
    }
}