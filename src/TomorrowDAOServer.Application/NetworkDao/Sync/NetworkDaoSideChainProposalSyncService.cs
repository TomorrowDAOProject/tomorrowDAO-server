using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Sync;

public class NetworkDaoSideChainProposalSyncService : ScheduleSyncDataService
{
    private readonly ILogger<NetworkDaoSideChainProposalSyncService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly INetworkDaoProposalSyncService _networkDaoProposalSyncService;
    private readonly IContractProvider _contractProvider;

    public NetworkDaoSideChainProposalSyncService(ILogger<NetworkDaoSideChainProposalSyncService> logger,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService, INetworkDaoProposalSyncService networkDaoProposalSyncService,
        IContractProvider contractProvider) : base(
        logger, graphQlProvider)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _networkDaoProposalSyncService = networkDaoProposalSyncService;
        _contractProvider = contractProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        return await _networkDaoProposalSyncService.SyncIndexerRecordsAsync(chainId, lastEndHeight, newIndexHeight);
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        return new List<string>() { _contractProvider.SideChainId() };
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.NetworkDaoSideChainSync;
    }
}