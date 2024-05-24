using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Provider;

namespace TomorrowDAOServer.Proposal;

public class ProposalNewUpdateService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IProposalProvider _proposalProvider;
    private readonly IChainAppService _chainAppService;
    private readonly IProposalAssistService _proposalAssistService;
    private readonly IScriptService _scriptService;

    public ProposalNewUpdateService(ILogger<ProposalSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        IProposalProvider proposalProvider,
        IChainAppService chainAppService,
        IProposalAssistService proposalAssistService,
        IScriptService scriptService)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _proposalProvider = proposalProvider;
        _chainAppService = chainAppService;
        _proposalAssistService = proposalAssistService;
        _scriptService = scriptService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        List<ProposalIndex> queryList;
        do
        {
            queryList = await _proposalProvider.GetNeedChangeProposalListAsync(skipCount);
            _logger.LogInformation("NeedChangeProposalList skipCount {skipCount} count: {count}", skipCount, queryList?.Count);
            var result = await _proposalAssistService.NewConvertProposalList(chainId, queryList);
            if (!result.IsNullOrEmpty())
            {
                await _proposalProvider.BulkAddOrUpdateAsync(result);
            }
            skipCount += result.Count;
        } while (!queryList.IsNullOrEmpty());

        return -1L;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.ProposalNewUpdate;
    }
}