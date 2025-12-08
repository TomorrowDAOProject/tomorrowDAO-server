using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Sync;

public class NetworkDaoSideChainOrgSyncService : ScheduleSyncDataService
{
    private readonly ILogger<NetworkDaoSideChainProposalSyncService> _logger;
    private readonly INetworkDaoOrgSyncService _networkDaoOrgSyncService;
    private readonly IContractProvider _contractProvider;

    public NetworkDaoSideChainOrgSyncService(ILogger<ScheduleSyncDataService> logger, IGraphQLProvider graphQlProvider,
        ILogger<NetworkDaoSideChainProposalSyncService> logger1, INetworkDaoOrgSyncService networkDaoOrgSyncService,
        IContractProvider contractProvider) : base(logger, graphQlProvider)
    {
        _logger = logger1;
        _networkDaoOrgSyncService = networkDaoOrgSyncService;
        _contractProvider = contractProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        return await _networkDaoOrgSyncService.SyncIndexerRecordsAsync(chainId, lastEndHeight, newIndexHeight);
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        return new List<string>() { _contractProvider.SideChainId() };
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.NetworkDaoSideChainOrgSync;
    }
}