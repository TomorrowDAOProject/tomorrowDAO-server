using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Treasury;
using TomorrowDAOServer.Treasury.Dto;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Treasury")]
[Route("api/app/treasury")]
public class TreasuryControllers : AbpController
{
    private readonly ILogger<TreasuryControllers> _logger;
    private readonly ITreasuryAssetsService _treasuryAssetsService;

    public TreasuryControllers(ILogger<TreasuryControllers> logger, ITreasuryAssetsService treasuryAssetsService)
    {
        _logger = logger;
        _treasuryAssetsService = treasuryAssetsService;
    }

    [HttpPost("assets")]
    public async Task<TreasuryAssetsPagedResultDto> GetTreasuryAssetsAsync(GetTreasuryAssetsInput input)
    {
        return await _treasuryAssetsService.GetTreasuryAssetsAsync(input);
    }
    
    [HttpPost("is-depositor")]
    public async Task<bool> IsTreasuryDepositorAsync(IsTreasuryDepositorInput input)
    {
        return await _treasuryAssetsService.IsTreasuryDepositorAsync(input);
    }
    
    [HttpGet("address")]
    public async Task<string> GetTreasuryAddressAsync(GetTreasuryAddressInput input)
    {
        return await _treasuryAssetsService.GetTreasuryAddressAsync(input);
    }
    
    [HttpGet("records")]
    public async Task<PageResultDto<TreasuryRecordDto>> GetTreasuryRecordsAsync(GetTreasuryRecordsInput input)
    {
        return await _treasuryAssetsService.GetTreasuryRecordsAsync(input);
    }
}