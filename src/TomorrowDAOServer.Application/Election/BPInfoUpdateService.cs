using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Election;

public class BPInfoUpdateService : ScheduleSyncDataService
{
    private readonly ILogger<BPInfoUpdateService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly IScriptService _scriptService;
    private readonly IGraphQLProvider _graphQlProvider;
    
    public BPInfoUpdateService(ILogger<BPInfoUpdateService> logger, IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService, IScriptService scriptService)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _scriptService = scriptService;
        _graphQlProvider = graphQlProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var bpList = await _scriptService.GetCurrentBPAsync(chainId);
        var round = await _scriptService.GetCurrentBPRoundAsync(chainId);
        Log.Information("bpList count {count} round {round}", bpList.Count, round);
        await _graphQlProvider.SetBPAsync(chainId, bpList, round);
        return newIndexHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        //add multiple chains
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.BPInfoUpdate;
    }
}