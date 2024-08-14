using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Spider;
using TomorrowDAOServer.Telegram;
using TomorrowDAOServer.Telegram.Dto;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Forum")]
[Route("api/app/telegram")]
public class TelegramController : AbpController
{
    private readonly ILogger<TreasuryControllers> _logger;
    private readonly ITelegramAppsSpiderService _telegramAppsSpiderService;
    private readonly ITelegramService _telegramService;


    public TelegramController(ILogger<TreasuryControllers> logger, ITelegramAppsSpiderService telegramAppsSpiderService,
        ITelegramService telegramService)
    {
        _logger = logger;
        _telegramAppsSpiderService = telegramAppsSpiderService;
        _telegramService = telegramService;
    }

    [HttpGet("load")]
    [Authorize]
    public async Task<List<TelegramAppDto>> LoadTelegramAppsAsync(LoadTelegramAppsInput input)
    {
        var telegramAppDtos = await _telegramAppsSpiderService.LoadTelegramAppsAsync(input);

        await _telegramService.SaveTelegramAppsAsync(telegramAppDtos);
        return telegramAppDtos;
    }
    
    [HttpPost("save")]
    [Authorize]
    public async Task<bool> LoadTelegramAppsAsync(SaveTelegramAppsInput input)
    {
        await _telegramService.SaveTelegramAppAsync(input.TelegramAppDto, input.ChainId);
        return true;
    }

    [HttpPost("apps")]
    public async Task<List<TelegramAppDto>> GetTelegramAppsAsync(QueryTelegramAppsInput input)
    {
        return await _telegramService.GetTelegramAppAsync(input);
    }
}