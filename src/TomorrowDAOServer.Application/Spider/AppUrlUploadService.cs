using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.File;
using TomorrowDAOServer.Telegram.Dto;
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
            
            var toUpdate = new List<TelegramAppIndex>();
            foreach (var index in queryList)
            {
                var needUpdate = false;
                var icon = index.Icon;
                var screenshots = index.Screenshots ?? new List<string>();
                var backScreenshots = index.BackScreenshots ?? new List<string>();
                if (NeedUpload(icon, index.BackIcon))
                {
                    icon = GetUrl(icon);
                    var backIcon = await _fileService.UploadFrontEndAsync(icon, Guid.NewGuid().ToString("N").ToUpper());
                    if (!string.IsNullOrEmpty(backIcon))
                    {
                        needUpdate = true;
                        index.BackIcon = backIcon;
                    }
                }

                if (NeedUpload(screenshots, backScreenshots))
                {
                    var newBackScreenshots = new List<string>();
                    foreach (var screenshot in screenshots)
                    {
                        var backScreenshot = await _fileService.UploadFrontEndAsync(screenshot, Guid.NewGuid().ToString("N").ToUpper());
                        if (!string.IsNullOrEmpty(backScreenshot))
                        {
                            newBackScreenshots.Add(backScreenshot);
                        }
                    }
                    if (newBackScreenshots.Any())
                    {
                        needUpdate = true;
                        index.BackScreenshots = newBackScreenshots;
                    }
                }

                if (needUpdate)
                {
                    toUpdate.Add(index);
                }
            }
            
            _logger.LogInformation("AppUrlUploadNeedUpdate allCount {0} updateCount {1} skipCount {2}", queryList.Count, toUpdate.Count, skipCount);
            await _telegramAppsProvider.BulkAddOrUpdateAsync(toUpdate);
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

    public string GetUrl(string url)
    {
        return url.StartsWith("/") ? CommonConstant.FindminiUrlPrefix + url : url;
    }

    public bool NeedUpload(string icon, string backIcon)
    {
        if (string.IsNullOrEmpty(icon))
        {
            return false;
        }

        return string.IsNullOrEmpty(backIcon);
    }

    public bool NeedUpload(List<string> screenshots, List<string> backScreenshots)
    {
        if (screenshots == null || !screenshots.Any())
        {
            return false;
        }

        if (backScreenshots == null || !backScreenshots.Any())
        {
            return true;
        }

        return false;
    }
}