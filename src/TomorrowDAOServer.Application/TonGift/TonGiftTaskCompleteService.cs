using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aetherlink.PriceServer.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.TonGift.Provider;

namespace TomorrowDAOServer.TonGift;

public class TonGiftTaskCompleteService: ScheduleSyncDataService
{
    private readonly ILogger<TonGiftTaskCompleteService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly ITonGiftTaskProvider _tonGiftTaskProvider;
    private readonly IOptionsMonitor<TonGiftTaskOptions> _tonGiftTaskOptions;
    private readonly ITonGiftApiProvider _tonGiftApiProvider;
    
    public TonGiftTaskCompleteService(ILogger<TonGiftTaskCompleteService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, ITonGiftTaskProvider tonGiftTaskProvider, 
        IOptionsMonitor<TonGiftTaskOptions> tonGiftTaskOptions, 
        ITonGiftApiProvider tonGiftApiProvider) : base(logger, graphQlProvider)
    {
        _chainAppService = chainAppService;
        _tonGiftTaskProvider = tonGiftTaskProvider;
        _tonGiftTaskOptions = tonGiftTaskOptions;
        _tonGiftApiProvider = tonGiftApiProvider;
        _logger = logger;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        var taskId = _tonGiftTaskOptions.CurrentValue.TaskId;
        List<TonGiftTaskIndex> queryList;

        do
        {
            queryList = await _tonGiftTaskProvider.GetNeedUpdateListAsync(taskId, skipCount);
            _logger.LogInformation("NeedChangeProposalList taskId {taskId} skipCount {skipCount} count: {count}", taskId, skipCount, queryList?.Count);

            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }

            var dic = queryList.ToDictionary(index => index.Identifier, index => Tuple.Create(index.CaHash, index.IdentifierHash, index.Address));
            var identifiers = queryList.Select(x => x.Identifier).Distinct().ToList();
            var response = await _tonGiftApiProvider.UpdateTaskAsync(identifiers);
            var toAdd = new List<TonGiftTaskIndex>();

            if (response.Message.Contains("successfully"))
            {
                toAdd.AddRange(identifiers.Select(x => CreateTask(x, dic[x], UpdateTaskStatus.Completed)).ToList());
            }
            else
            {
                toAdd.AddRange(response.SuccessfulUpdates.Select(x => CreateTask(x.UserId, dic[x.UserId], UpdateTaskStatus.Completed)).ToList());
                toAdd.AddRange(response.FailedUpdates.Select(x => CreateTask(x.UserId, dic[x.UserId], UpdateTaskStatus.Failed)).ToList());
            }

            await _tonGiftTaskProvider.BulkAddOrUpdateAsync(toAdd);
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
        return WorkerBusinessType.TonGiftTaskComplete;
    }
    
    private TonGiftTaskIndex CreateTask(string identifier, Tuple<string, string, string> identifierInfo, UpdateTaskStatus status)
    {
        var taskId = _tonGiftTaskOptions.CurrentValue.TaskId;
        var (caHash, identifierHash, address) = identifierInfo;
        return new TonGiftTaskIndex
        {
            Id = IdGeneratorHelper.GenerateId(taskId, address, identifier),
            TaskId = taskId,
            Identifier = identifier,
            CaHash = caHash,
            IdentifierHash = identifierHash,
            Address = address,
            TonGiftTask = TonGiftTask.Vote,
            UpdateTaskStatus = status
        };
    }
}