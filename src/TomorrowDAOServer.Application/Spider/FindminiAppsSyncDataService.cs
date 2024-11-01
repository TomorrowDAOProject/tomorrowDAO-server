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
    private readonly ILogger<FindminiAppsSyncDataService> _logger;
    
    public FindminiAppsSyncDataService(ILogger<ScheduleSyncDataService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, IFindminiAppsSpiderService findminiAppsSpiderService, 
        IOptionsMonitor<TelegramOptions> telegramOptions, ITelegramService telegramService, ILogger<FindminiAppsSyncDataService> logger1) : base(logger, graphQlProvider)
    {
        _chainAppService = chainAppService;
        _findminiAppsSpiderService = findminiAppsSpiderService;
        _telegramOptions = telegramOptions;
        _telegramService = telegramService;
        _logger = logger1;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var hour =  _telegramOptions.CurrentValue.FindminiSpiderTime;
        if (TimeHelper.IsTimestampToday(lastEndHeight) && DateTime.UtcNow.Hour != hour)
        {
            _logger.LogInformation("FindminiNoNeedToSync");
            return lastEndHeight;
        }
        _logger.LogInformation("FindminiNeedToSyncStart");
        var categoryList = _telegramOptions.CurrentValue.FindMiniCategoryList;
        foreach (var url in categoryList.Select(category => CommonConstant.FindminiCategoryPrefix + category + "/"))
        {
            for (var i = 1; i < 50; i++)
            {
                var pageUrl = i == 1 ? url : url + i;
                _logger.LogInformation("FindminiSyncStart url={url}", pageUrl);
                var apps = await _findminiAppsSpiderService.LoadAsync(pageUrl);
                if (apps.IsNullOrEmpty())
                {
                    _logger.LogInformation("FindminiSyncBreak url={url}", pageUrl);
                    break;
                }
                _logger.LogInformation("FindminiSyncEnd url={url}, count={count}", pageUrl, apps.Count);
                await _telegramService.SaveNewTelegramAppsAsync(apps);
            }
        }
        _logger.LogInformation("FindminiNeedToSyncEnd");
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