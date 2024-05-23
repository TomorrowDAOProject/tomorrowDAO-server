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
            var tasks = queryList!.Select(async proposal =>
            {
                var result = await _scriptService.GetProposalInfoAsync(chainId, proposal.ProposalId);
                if (result != null)
                {
                    proposal.ProposalStage = Enum.Parse<ProposalStage>(Convert(result.ProposalStage));
                    proposal.ProposalStatus = result.ProposalStatus switch
                    {
                        "PENDING_VOTE" => ProposalStatus.PendingVote,
                        "BELOW_THRESHOLD" => ProposalStatus.BelowThreshold,
                        _ => Enum.Parse<ProposalStatus>(Convert(result.ProposalStatus))
                    };
                }
            }).ToArray();
            await Task.WhenAll(tasks);
            if (!queryList.IsNullOrEmpty())
            {
                await _proposalProvider.BulkAddOrUpdateAsync(queryList);
            }
            skipCount += queryList.Count;
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

    private static string Convert(string enumStr)
    {
        if (string.IsNullOrEmpty(enumStr))
        {
            return enumStr;
        }

        if (enumStr.Length == 1)
        {
            return enumStr.ToUpper();
        }

        return enumStr[..1].ToUpper() + enumStr[1..].ToLower();
    }
}