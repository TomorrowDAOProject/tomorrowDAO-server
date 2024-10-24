using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.ResourceToken.Provider;

namespace TomorrowDAOServer.ResourceToken;

public class ResourceTokenParseService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IResourceTokenProvider _resourceTokenProvider;
    private readonly IExplorerProvider _explorerProvider;
    
    public ResourceTokenParseService(ILogger<DAOSyncDataService> logger, IGraphQLProvider graphQlProvider,
        IResourceTokenProvider resourceTokenProvider, IExplorerProvider explorerProvider)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _resourceTokenProvider = resourceTokenProvider;
        _explorerProvider = explorerProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        List<ResourceTokenIndex> queryList;
        do
        {
            queryList = await _resourceTokenProvider.GetNeedParseAsync(skipCount);
            _logger.LogInformation("NeedParseResourceTokenList skipCount {skipCount} count: {count}", skipCount, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            foreach (var index in queryList)
            {
                var txId = index.TransactionId;
                var transaction = await _explorerProvider.GetTransactionDetailAsync(
                    CommonConstant.MainChainId, new ExplorerTransactionDetailRequest { ChainId = CommonConstant.MainChainId, TransactionId = txId });
                if (transaction?.List == null || transaction.List.IsNullOrEmpty())
                {
                    continue;
                }
                var transferDetail = transaction.List[0].TokenTransferreds
                    .FirstOrDefault(x => x.To.Name == SystemContractName.TokenConverterContract);
                index.Address = transferDetail?.From?.Address ?? string.Empty;
            }
            
            await _resourceTokenProvider.BulkAddOrUpdateAsync(queryList);
            skipCount += queryList.Count;
        } while (!queryList.IsNullOrEmpty());

        return -1L;
    }

    public override Task<List<string>> GetChainIdsAsync()
    {
        return Task.FromResult(new List<string> { CommonConstant.MainChainId });
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.ResourceTokenParse;
    }
}