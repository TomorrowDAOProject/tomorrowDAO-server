using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Provider;
using TomorrowDAOServer.Providers;

namespace TomorrowDAOServer.Proposal;

public class ProposalNumUpdateService : ScheduleSyncDataService
{
    private readonly ILogger<ProposalNumUpdateService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INetworkDaoEsDataProvider _networkDaoEsDataProvider;
    
    public ProposalNumUpdateService(ILogger<ProposalNumUpdateService> logger,
        IGraphQLProvider graphQlProvider, IChainAppService chainAppService, IExplorerProvider explorerProvider,
        INetworkDaoEsDataProvider networkDaoEsDataProvider)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _chainAppService = chainAppService;
        _explorerProvider = explorerProvider;
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var parliamentTask = GetCountTask(NetworkDaoOrgType.Parliament);
        var associationTask = GetCountTask(NetworkDaoOrgType.Association);
        var referendumTask = GetCountTask(NetworkDaoOrgType.Referendum);
        await Task.WhenAll(parliamentTask, associationTask, referendumTask);
        var parliamentCount = parliamentTask.Result.Item1;
        var associationCount = associationTask.Result.Item1;
        var referendumCount = referendumTask.Result.Item1;
        Log.Information("ProposalNumUpdate parliamentCount {parliamentCount}, associationCount {associationCount}, referendumCount {referendumCount}",
            parliamentCount, associationCount, referendumCount);
        await _graphQlProvider.SetProposalNumAsync(chainId, parliamentCount, associationCount, referendumCount);
        return -1;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.ProposalNumUpdate;
    }
    
    private async Task<Tuple<long, List<NetworkDaoProposalIndex>>> GetCountTask(NetworkDaoOrgType type)
    {
        return await _networkDaoEsDataProvider.GetProposalListAsync(new GetProposalListInput
        {
            ChainId = CommonConstant.MainChainId,
            IsContract = false,
            Status = NetworkDaoProposalStatusEnum.All,
            ProposalType = type
        });
    }
}