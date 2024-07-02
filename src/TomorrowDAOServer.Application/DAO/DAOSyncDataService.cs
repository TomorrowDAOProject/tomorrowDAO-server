using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Enums;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.DAO;

public class DAOSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IDAOProvider _daoProvider;
    private readonly IChainAppService _chainAppService;
    private readonly INESTRepository<DAOIndex, string> _daoIndexRepository;
    private readonly IDaoAliasProvider _daoAliasProvider;
    private const int MaxResultCount = 500;
    
    public DAOSyncDataService(ILogger<DAOSyncDataService> logger,
        IObjectMapper objectMapper, 
        IGraphQLProvider graphQlProvider,
        IDAOProvider daoProvider,
        IChainAppService chainAppService,
        INESTRepository<DAOIndex, string> daoIndexRepository, IDaoAliasProvider daoAliasProvider)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _daoProvider = daoProvider;
        _chainAppService = chainAppService;
        _daoIndexRepository = daoIndexRepository;
        _daoAliasProvider = daoAliasProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        //var blockHeight = -1L;
        List<IndexerDAOInfo> queryList;
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
            queryList = await _daoProvider.GetSyncDAOListAsync(input);
            _logger.LogInformation("SyncDAOInfos queryList chainId: {chainId} skipCount: {skipCount} startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
                chainId, skipCount, lastEndHeight, newIndexHeight, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }
            //blockHeight = Math.Max(blockHeight, queryList.Select(t => t.BlockHeight).Max());
            var indexerDaoInfos = queryList.ToDictionary(x => x.Id);
            var daoIndices = _objectMapper.Map<List<IndexerDAOInfo>, List<DAOIndex>>(queryList);
            foreach (var daoIndex in daoIndices)
            {
                if (indexerDaoInfos.TryGetValue(daoIndex.Id, out var indexerDaoInfo))
                {
                    daoIndex.HighCouncilConfig = new HighCouncilConfig
                    {
                        MaxHighCouncilCandidateCount = indexerDaoInfo.MaxHighCouncilCandidateCount,
                        MaxHighCouncilMemberCount = indexerDaoInfo.MaxHighCouncilMemberCount,
                        ElectionPeriod = indexerDaoInfo.ElectionPeriod,
                        StakingAmount = indexerDaoInfo.StakingAmount
                    };
                }
                else
                {
                    daoIndex.HighCouncilConfig = new HighCouncilConfig();
                }
                //generate alias
                daoIndex.Alias = await _daoAliasProvider.GenerateDaoAliasAsync(daoIndex);
            }
            await _daoIndexRepository.BulkAddOrUpdateAsync(daoIndices);
            skipCount += queryList.Count;
        } while (!queryList.IsNullOrEmpty());

        return newIndexHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.DAOSync;
    }
}