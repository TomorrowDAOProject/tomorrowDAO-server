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
using TomorrowDAOServer.User.Provider;
using TomorrowDAOServer.Users.Indexer;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.User;

public class UserBalanceSyncDataService : ScheduleSyncDataService
{
    private readonly IChainAppService _chainAppService;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IObjectMapper _objectMapper;
    private const int MaxResultCount = 500;
    public UserBalanceSyncDataService(ILogger<UserBalanceSyncDataService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, IUserBalanceProvider userBalanceProvider, IObjectMapper objectMapper) : base(logger, graphQlProvider)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _userBalanceProvider = userBalanceProvider;
        _objectMapper = objectMapper;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        var blockHeight = -1L;
        List<UserBalance> queryList;
        do
        {
            var input = new GetChainBlockHeightInput
            {
                ChainId = chainId,
                SkipCount = skipCount,
                MaxResultCount = MaxResultCount,
                StartBlockHeight = lastEndHeight,
                EndBlockHeight = newIndexHeight
            };
            queryList = await _userBalanceProvider.GetSyncUserBalanceListAsync(input);
            _logger.LogInformation("SyncUserBalance queryList chainId: {chainId} skipCount: {skipCount} startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
                chainId, skipCount, lastEndHeight, newIndexHeight, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            blockHeight = Math.Max(blockHeight, queryList.Select(t => t.BlockHeight).Max());
            await _userBalanceProvider.BulkAddOrUpdateAsync(_objectMapper.Map<List<UserBalance>, List<UserBalanceIndex>>(queryList));
            skipCount += queryList.Count;
        } while (!queryList.IsNullOrEmpty());

        return blockHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.UserBalanceSync;
    }
}