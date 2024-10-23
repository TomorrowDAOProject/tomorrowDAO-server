using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.ResourceToken.Provider;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.ResourceToken;

public class ResourceTokenParseService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IChainAppService _chainAppService;
    private readonly IResourceTokenProvider _resourceTokenProvider;
    private readonly ITransactionService _transactionService;
    
    public ResourceTokenParseService(ILogger<DAOSyncDataService> logger, IObjectMapper objectMapper,
        IGraphQLProvider graphQlProvider, IChainAppService chainAppService, IResourceTokenProvider resourceTokenProvider, 
        ITransactionService transactionService)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _chainAppService = chainAppService;
        _resourceTokenProvider = resourceTokenProvider;
        _transactionService = transactionService;
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
                var transaction = await _transactionService.GetTransactionById(chainId, txId);
                index.Address = transaction?.Transaction?.From ?? string.Empty;
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