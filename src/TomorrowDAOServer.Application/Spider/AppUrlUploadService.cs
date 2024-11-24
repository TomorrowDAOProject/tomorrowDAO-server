using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.File;
using TomorrowDAOServer.Telegram.Provider;

namespace TomorrowDAOServer.Spider;

public class AppUrlUploadService : ScheduleSyncDataService
{
    private readonly IChainAppService _chainAppService;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IFileService _fileService;
    
    public AppUrlUploadService(ILogger<ScheduleSyncDataService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, ITelegramAppsProvider telegramAppsProvider, IFileService fileService) 
        : base(logger, graphQlProvider)
    {
        _chainAppService = chainAppService;
        _telegramAppsProvider = telegramAppsProvider;
        _fileService = fileService;
        _logger = logger;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        List<TelegramAppIndex> queryList;
        do
        {
            queryList = await _telegramAppsProvider.GetNeedUploadAsync(skipCount);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            foreach (var index in queryList.Where(x => !string.IsNullOrEmpty(x.Url) && string.IsNullOrEmpty(x.BackUrl)))
            {
                var url = index.Url;
                var uri = new Uri(url);
                var extension = System.IO.Path.GetExtension(uri.LocalPath);
                var backUrl = await _fileService.UploadAsync(url, "url_" + index.Id + extension);
                index.BackUrl = backUrl;
            }
            
            foreach (var index in queryList.Where(x => x.Screenshots != null && x.Screenshots.Any() 
                       && (x.BackScreenshots == null || !x.BackScreenshots.Any())))
            {
                var screenshots = index.Screenshots;
                var backScreenshots = new List<string>();
                for (var i = 0; i < screenshots.Count; i++)
                {
                    var screenshot = screenshots[i];
                    var uri = new Uri(screenshot);
                    var extension = System.IO.Path.GetExtension(uri.LocalPath);
                    var backScreenshot = await _fileService.UploadAsync(screenshot, "screenshot_"+ i + "_" + index.Id  + extension);
                    backScreenshots.Add(backScreenshot);
                }
                index.Screenshots = backScreenshots;
            }
            await _telegramAppsProvider.BulkAddOrUpdateAsync(queryList);
            skipCount += queryList.Count;
        } while (!queryList.IsNullOrEmpty());
        return -1L;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.AppUrlUpload;
    }
}