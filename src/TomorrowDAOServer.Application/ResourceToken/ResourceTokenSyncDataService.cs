using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.ResourceToken.Indexer;
using TomorrowDAOServer.ResourceToken.Provider;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.ResourceToken;

public class ResourceTokenSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IChainAppService _chainAppService;
    private readonly IResourceTokenProvider _resourceTokenProvider;
    private const int MaxResultCount = 500;
    
    public ResourceTokenSyncDataService(ILogger<DAOSyncDataService> logger, IObjectMapper objectMapper,
        IGraphQLProvider graphQlProvider, IChainAppService chainAppService, IResourceTokenProvider resourceTokenProvider)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _chainAppService = chainAppService;
        _resourceTokenProvider = resourceTokenProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        var blockHeight = -1L;
        List<IndexerResourceTokenDto> queryList;
        do
        {
            queryList = await _resourceTokenProvider.GetSyncResourceTokenDataAsync(skipCount, chainId, lastEndHeight, 0, MaxResultCount);
            _logger.LogInformation("SyncResourceTokenInfos queryList chainId: {chainId} skipCount: {skipCount} startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
                chainId, skipCount, lastEndHeight, newIndexHeight, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            blockHeight = Math.Max(blockHeight, queryList.Select(t => t.BlockHeight).Max());
            var ids = queryList.Select(x => x.Id).ToList();
            var exists = await _resourceTokenProvider.GetByIdsAsync(ids);
            var toAdd = queryList
                .Where(item => exists.All(e => e.Id != item.Id)).ToList();
            await _resourceTokenProvider.BulkAddOrUpdateAsync(
                _objectMapper.Map<List<IndexerResourceTokenDto>, List<ResourceTokenIndex>>(toAdd));
            skipCount += queryList.Count;
        } while (!queryList.IsNullOrEmpty());

        return blockHeight;
    }

    public override Task<List<string>> GetChainIdsAsync()
    {
        return Task.FromResult(new List<string> { CommonConstant.MainChainId });
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.ResourceTokenSync;
    }
}