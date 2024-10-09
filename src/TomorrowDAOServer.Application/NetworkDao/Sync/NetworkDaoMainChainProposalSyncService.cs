using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Sync;

public class NetworkDaoMainChainProposalSyncService : ScheduleSyncDataService
{
    private readonly ILogger<NetworkDaoMainChainProposalSyncService> _logger;
    private readonly INetworkDaoProposalSyncService _networkDaoProposalSyncService;
    private readonly IContractProvider _contractProvider;

    public NetworkDaoMainChainProposalSyncService(ILogger<NetworkDaoMainChainProposalSyncService> logger,
        IGraphQLProvider graphQlProvider,
        INetworkDaoProposalSyncService networkDaoProposalSyncService,
        IContractProvider contractProvider) : base(
        logger, graphQlProvider)
    {
        _logger = logger;
        _networkDaoProposalSyncService = networkDaoProposalSyncService;
        _contractProvider = contractProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        return await _networkDaoProposalSyncService.SyncIndexerRecordsAsync(chainId, lastEndHeight, newIndexHeight);
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        return new List<string>() { _contractProvider.MainChainId() };
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.NetworkDaoMainChainSync;
    }
}