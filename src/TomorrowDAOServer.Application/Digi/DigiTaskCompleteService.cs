using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Digi.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.LuckyBox.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Telegram.Provider;

namespace TomorrowDAOServer.Digi;

public class DigiTaskCompleteService : ScheduleSyncDataService
{
    private readonly ILogger<DigiTaskCompleteService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly ITelegramUserInfoProvider _telegramUserInfoProvider;
    private readonly IDigiTaskProvider _digiTaskProvider;
    private readonly IDigiApiProvider _digiApiProvider;
    private readonly IOptionsMonitor<DigiOptions> _digiOptions;
    
    public DigiTaskCompleteService(ILogger<DigiTaskCompleteService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, IOptionsMonitor<DigiOptions> digiOptions, IDigiTaskProvider digiTaskProvider,
        ITelegramUserInfoProvider telegramUserInfoProvider, IDigiApiProvider digiApiProvider) 
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _digiOptions = digiOptions;
        _digiTaskProvider = digiTaskProvider;
        _telegramUserInfoProvider = telegramUserInfoProvider;
        _digiApiProvider = digiApiProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var start = _digiOptions.CurrentValue.Start;
        if (!start)
        {
            _logger.LogInformation("DigiTaskNotStart");
            return 1L;
        }
        
        var skipCount = 0;
        List<DigiTaskIndex> queryList;
        var startTime = _digiOptions.CurrentValue.StartTime;
        do
        {
            queryList = await _digiTaskProvider.GetNeedReportAsync(skipCount, startTime);
            _logger.LogInformation("NeedReportDigiTaskAsync skipCount {skipCount} count: {count}", skipCount, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            var missingTelegramIdTasks = queryList.Where(task => string.IsNullOrEmpty(task.TelegramId)).ToList();
            if (missingTelegramIdTasks.Count != 0)
            {
                var addressList = missingTelegramIdTasks.Select(task => task.Address).ToList();
                var userInfoList = await _telegramUserInfoProvider.GetByAddressListAsync(addressList);
                var userInfoDict = userInfoList.ToDictionary(info => info.Address, info => info.TelegramId);
                foreach (var task in missingTelegramIdTasks)
                {
                    if (userInfoDict.TryGetValue(task.Address, out var telegramId))
                    {
                        task.TelegramId = telegramId;
                    }
                }
            }
            var validList = queryList.Where(task => !string.IsNullOrEmpty(task.TelegramId)).ToList();
            var checkTasks = validList.Select(async task =>
            {
                var result = await _digiApiProvider.CheckAsync(task.TelegramId);
                task.UpdateTaskStatus = result ? UpdateTaskStatus.Completed : UpdateTaskStatus.Failed;
            });
            await Task.WhenAll(checkTasks);
            await _digiTaskProvider.BulkAddOrUpdateAsync(queryList);
            skipCount += queryList.Count;
        } while (!queryList.IsNullOrEmpty());

        return 1L;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.DigiTaskComplete;
    }
}