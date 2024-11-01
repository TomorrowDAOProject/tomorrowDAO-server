using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Telegram;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Spider;

public class TelegramAppsSyncDataService : ScheduleSyncDataService
{
    private readonly IChainAppService _chainAppService;
    private readonly ITelegramAppsSpiderService _telegramAppsSpiderService;
    private readonly ITelegramService _telegramService;
    private readonly ILogger<TelegramAppsSyncDataService> _logger;
    private readonly IOptionsMonitor<TelegramOptions> _telegramOptions;
    
    public TelegramAppsSyncDataService(ILogger<ScheduleSyncDataService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, ITelegramAppsSpiderService telegramAppsSpiderService, 
        ITelegramService telegramService, ILogger<TelegramAppsSyncDataService> logger1, 
        IOptionsMonitor<TelegramOptions> telegramOptions) : base(logger, graphQlProvider)
    {
        _chainAppService = chainAppService;
        _telegramAppsSpiderService = telegramAppsSpiderService;
        _telegramService = telegramService;
        _logger = logger1;
        _telegramOptions = telegramOptions;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        if (TimeHelper.IsTimestampToday(lastEndHeight))
        {
            _logger.LogInformation("TelegramNoNeedToSync");
            return lastEndHeight;
        }
        var telegramAppDtos = await _telegramAppsSpiderService.LoadAllTelegramAppsAsync(new LoadAllTelegramAppsInput { ChainId = chainId }, false);
        _logger.LogInformation("TelegramSyncBasicEnd count={0}", telegramAppDtos.Count);
        await _telegramService.SaveNewTelegramAppsAsync(telegramAppDtos);
        _logger.LogInformation("TelegramSyncDetailStart");
        var telegramAppDetailDtos = await _telegramAppsSpiderService.LoadAllTelegramAppsDetailAsync(chainId, false);
        _logger.LogInformation("TelegramSyncDetailEnd count={0}", telegramAppDetailDtos.Count);
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