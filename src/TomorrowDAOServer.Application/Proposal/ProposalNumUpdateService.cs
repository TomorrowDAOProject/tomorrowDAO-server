using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Providers;

namespace TomorrowDAOServer.Proposal;

public class ProposalNumUpdateService : ScheduleSyncDataService
{
    private readonly ILogger<ProposalNumUpdateService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    
    public ProposalNumUpdateService(ILogger<ProposalNumUpdateService> logger,
        IGraphQLProvider graphQlProvider, IChainAppService chainAppService, IExplorerProvider explorerProvider)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _chainAppService = chainAppService;
        _explorerProvider = explorerProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var parliamentTask = GetCountTask(Common.Enum.ProposalType.Parliament);
        var associationTask = GetCountTask(Common.Enum.ProposalType.Association);
        var referendumTask = GetCountTask(Common.Enum.ProposalType.Referendum);
        await Task.WhenAll(parliamentTask, associationTask, referendumTask);
        var parliamentCount = parliamentTask.Result.Total;
        var associationCount = associationTask.Result.Total;
        var referendumCount = referendumTask.Result.Total;
        _logger.LogInformation("ProposalNumUpdate parliamentCount {parliamentCount}, associationCount {associationCount}, referendumCount {referendumCount}",
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
    
    private Task<ExplorerProposalResponse> GetCountTask(Common.Enum.ProposalType type)
    {
        return _explorerProvider.GetProposalPagerAsync(CommonConstant.MainChainId, new ExplorerProposalListRequest
        {
            ProposalType = type.ToString(),
            Status = "all", IsContract = 0
        });
    }
}