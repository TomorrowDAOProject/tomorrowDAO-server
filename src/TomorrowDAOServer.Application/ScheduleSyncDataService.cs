using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using Microsoft.Extensions.Logging;
using Serilog;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer;

public abstract class ScheduleSyncDataService : IScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;

    protected ScheduleSyncDataService(ILogger<ScheduleSyncDataService> logger, IGraphQLProvider graphQlProvider)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
    }


    public async Task DealDataAsync()
    {
        var businessType = GetBusinessType();
        var chainIds = await GetChainIdsAsync();
        //handle multiple chains
        foreach (var chainId in chainIds)
        {
            await DealDataAsync(chainId, businessType);
        }
    }

    [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default,
        Message = "DealDataAsyncError", LogTargets = new []{"chainId", "businessType"})]
    public virtual async Task DealDataAsync(string chainId, WorkerBusinessType businessType)
    {
        var lastEndHeight = await _graphQlProvider.GetLastEndHeightAsync(chainId, businessType);
        if (lastEndHeight < 0)
        {
            Log.Information(
                "Skip deal data for businessType: {businessType} chainId: {chainId} lastEndHeight: {lastEndHeight}",
                businessType, chainId, lastEndHeight);
            return;
        }

        var newIndexHeight = await _graphQlProvider.GetIndexBlockHeightAsync(chainId);
        Log.Information(
            "Start deal data for businessType: {businessType} chainId: {chainId} lastEndHeight: {lastEndHeight} newIndexHeight: {newIndexHeight}",
            businessType, chainId, lastEndHeight, newIndexHeight);
        var blockHeight = await SyncIndexerRecordsAsync(chainId, lastEndHeight, newIndexHeight);

        if (blockHeight > 0)
        {
            await _graphQlProvider.SetLastEndHeightAsync(chainId, businessType, blockHeight);
            Log.Information(
                "End deal data for businessType: {businessType} chainId: {chainId} lastEndHeight: {BlockHeight}",
                businessType, chainId, blockHeight);
        }
    }

    public abstract Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight);

    /**
     * different businesses obtain different multiple chains
     */
    public abstract Task<List<string>> GetChainIdsAsync();

    public abstract WorkerBusinessType GetBusinessType();

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName,
        Message = "reset last end height error", LogTargets = new []{"chainId", "businessType", "blockHeight"})]
    public virtual async Task ResetLastEndHeightAsync(string chainId, WorkerBusinessType businessType, long blockHeight)
    {
        if (blockHeight > 0)
        {
            await _graphQlProvider.SetLastEndHeightAsync(chainId, businessType, blockHeight);
            Log.Information(
                "reset last end height for businessType: {businessType} chainId: {chainId} lastEndHeight: {BlockHeight}",
                businessType, chainId, blockHeight);
        }
    }
}