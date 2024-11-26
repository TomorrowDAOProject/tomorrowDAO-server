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
            
            var toUpdate = new List<TelegramAppIndex>();
            foreach (var index in queryList)
            {
                var needUpdate = false;
                var url = index.Url;
                var screendhots = index.Screenshots ?? new List<string>();
                var backScreenshots = index.BackScreenshots ?? new List<string>();
                if (!string.IsNullOrEmpty(url) && string.IsNullOrEmpty(index.BackUrl))
                {
                    var backUrl = await _fileService.UploadFrontEndAsync(url, Guid.NewGuid().ToString());
                    if (!string.IsNullOrEmpty(backUrl))
                    {
                        needUpdate = true;
                        index.BackUrl = backUrl;
                    }
                }

                if (screendhots.Any() && !backScreenshots.Any())
                {
                    var screenshots = screendhots;
                    var newBackScreenshots = new List<string>();
                    foreach (var screenshot in screenshots)
                    {
                        var backScreenshot = await _fileService.UploadFrontEndAsync(screenshot, Guid.NewGuid().ToString());
                        if (!string.IsNullOrEmpty(backScreenshot))
                        {
                            newBackScreenshots.Add(backScreenshot);
                        }
                    }
                    if (newBackScreenshots.Any())
                    {
                        needUpdate = true;
                        index.Screenshots = backScreenshots;
                    }
                }

                if (needUpdate)
                {
                    toUpdate.Add(index);
                }
            }
            
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
}