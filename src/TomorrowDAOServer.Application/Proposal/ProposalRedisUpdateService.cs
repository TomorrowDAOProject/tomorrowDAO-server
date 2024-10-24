using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Provider;

namespace TomorrowDAOServer.Proposal;

public class ProposalRedisUpdateService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IProposalProvider _proposalProvider;
    private readonly IChainAppService _chainAppService;
    private readonly IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;
    
    public ProposalRedisUpdateService(ILogger<ProposalRedisUpdateService> logger,
        IGraphQLProvider graphQlProvider, IProposalProvider proposalProvider, IChainAppService chainAppService,
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider) 
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _proposalProvider = proposalProvider;
        _chainAppService = chainAppService;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var defaultProposal = await _proposalProvider.GetDefaultProposalAsync(chainId);
        if (defaultProposal == null)
        {
            return -1L;
        }

        var proposalId = await _rankingAppPointsRedisProvider.GetDefaultRankingProposalIdAsync(chainId);
        if (proposalId != defaultProposal.ProposalId)
        {
            await _rankingAppPointsRedisProvider.GenerateRedisDefaultProposal(defaultProposal.ProposalId,
                defaultProposal.ProposalDescription, chainId);
            _logger.LogInformation("DefaultProposalRedisChange from {from} to {to}", proposalId, defaultProposal.ProposalId);
        }

        return -1L;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.ProposalRedisUpdate;
    }
}