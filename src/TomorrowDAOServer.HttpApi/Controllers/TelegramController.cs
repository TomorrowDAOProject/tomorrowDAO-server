using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Spider;
using TomorrowDAOServer.Telegram;
using TomorrowDAOServer.Telegram.Dto;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Telegram")]
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
    
    [HttpGet("set-category")]
    [Authorize]
    public async Task SetCategoryAsync(string chainId)
    { 
        await _telegramService.SetCategoryAsync(chainId);
    }
    
    [HttpGet("load-all")]
    [Authorize]
    public async Task LoadAllTelegramAppsAsync(LoadAllTelegramAppsInput input)
    { 
        var apps = await _telegramAppsSpiderService.LoadAllTelegramAppsAsync(input);
        await _telegramService.SaveNewTelegramAppsAsync(apps);
    }

    [HttpGet("load")]
    [Authorize]
    public async Task<List<TelegramAppDto>> LoadTelegramAppsAsync(LoadTelegramAppsInput input)
    {
        var telegramAppDtos = await _telegramAppsSpiderService.LoadTelegramAppsAsync(input);
        await _telegramService.SaveNewTelegramAppsAsync(telegramAppDtos);
        return telegramAppDtos;
    }

    [HttpPost("load/detail")]
    [Authorize]
    public async Task<IDictionary<string, TelegramAppDetailDto>> LoadTelegramAppsDetailAsync(LoadTelegramAppsDetailInput input)
    {
        var telegramAppDetailDtos = await _telegramAppsSpiderService.LoadTelegramAppsDetailAsync(input);
        return await _telegramService.SaveTelegramAppDetailAsync(telegramAppDetailDtos);
    }
    
    [HttpGet("load-all-detail")]
    [Authorize]
    public async Task LoadAllTelegramAppsDetailAsync(string chainId)
    {
        var telegramAppDetailDtos = await _telegramAppsSpiderService.LoadAllTelegramAppsDetailAsync(chainId);
        await _telegramService.SaveTelegramAppDetailAsync(telegramAppDetailDtos);
    }

    [HttpPost("save")]
    [Authorize]
    public async Task<List<string>> BatchSaveAppAsync(BatchSaveAppsInput input)
    {
        return await _telegramService.SaveTelegramAppAsync(input);
    }

    [HttpGet("search-app")]
    [Authorize]
    public async Task<PageResultDto<AppDetailDto>> GetAppListAsync(string title)
    {
        return await _telegramService.SearchAppAsync(title);
    }

    [HttpGet("apps")]
    public async Task<GetTelegramAppResultDto> GetTelegramAppsAsync(GetTelegramAppInput input)
    {
        return await _telegramService.GetTelegramAppsAsync(input);
    }

    [Obsolete("Running this method will overwrite the historical stats")]
    [HttpPost("add-app")]
    [Authorize]
    public async Task<bool> AddAppAsync(AddAppInput input)
    {
        return await _telegramService.AddAppAsync(input);
    }
}