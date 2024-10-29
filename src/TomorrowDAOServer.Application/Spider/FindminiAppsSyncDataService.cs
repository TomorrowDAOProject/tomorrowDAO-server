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

namespace TomorrowDAOServer.Spider;

public class FindminiAppsSyncDataService : ScheduleSyncDataService
{
    private readonly IChainAppService _chainAppService;
    private readonly IFindminiAppsSpiderService _findminiAppsSpiderService;
    private readonly IOptionsMonitor<TelegramOptions> _telegramOptions;
    private readonly ITelegramService _telegramService;
    
    public FindminiAppsSyncDataService(ILogger<ScheduleSyncDataService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, IFindminiAppsSpiderService findminiAppsSpiderService, 
        IOptionsMonitor<TelegramOptions> telegramOptions, ITelegramService telegramService) : base(logger, graphQlProvider)
    {
        _chainAppService = chainAppService;
        _findminiAppsSpiderService = findminiAppsSpiderService;
        _telegramOptions = telegramOptions;
        _telegramService = telegramService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        if (TimeHelper.IsTimestampToday(lastEndHeight))
        {
            return lastEndHeight;
        }
        var categoryList = _telegramOptions.CurrentValue.FindMiniCategoryList;
        foreach (var url in categoryList.Select(category => "https://www.findmini.app/category/" + category))
        {
            var apps = await _findminiAppsSpiderService.LoadAsync(url);
            await _telegramService.SaveNewTelegramAppsAsync(apps);
            for (var i = 2; i < 40; i++)
            {
                var pageUrl = url + "/" + i + "/";
                apps = await _findminiAppsSpiderService.LoadAsync(pageUrl);
                if (apps.IsNullOrEmpty())
                {
                    break;
                }
                await _telegramService.SaveNewTelegramAppsAsync(apps);
            }
        }
        
        return DateTime.UtcNow.ToUtcMilliSeconds();
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.FindminiAppsSync;
    }
}