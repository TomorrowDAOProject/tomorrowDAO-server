using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Telegram;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Spider;

public class TelegramAppsSyncDataService : ScheduleSyncDataService
{
    private readonly IChainAppService _chainAppService;
    private readonly ITelegramAppsSpiderService _telegramAppsSpiderService;
    private readonly ITelegramService _telegramService;
    
    public TelegramAppsSyncDataService(ILogger<ScheduleSyncDataService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, ITelegramAppsSpiderService telegramAppsSpiderService, 
        ITelegramService telegramService) : base(logger, graphQlProvider)
    {
        _chainAppService = chainAppService;
        _telegramAppsSpiderService = telegramAppsSpiderService;
        _telegramService = telegramService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        // if (TimeHelper.IsTimestampToday(lastEndHeight))
        // {
        //     return lastEndHeight;
        // }
        var telegramAppDtos = await _telegramAppsSpiderService.LoadAllTelegramAppsAsync(new LoadAllTelegramAppsInput { ChainId = chainId }, false);
        await _telegramService.SaveNewTelegramAppsAsync(telegramAppDtos);
        var telegramAppDetailDtos = await _telegramAppsSpiderService.LoadAllTelegramAppsDetailAsync(chainId, false);
        await _telegramService.SaveTelegramAppDetailAsync(telegramAppDetailDtos);
        return DateTime.UtcNow.ToUtcMilliSeconds();
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.TelegramAppsSync;
    }
}