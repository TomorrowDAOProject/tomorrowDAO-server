using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Election;

public class HighCouncilMemberSyncService : ScheduleSyncDataService
{
    private readonly ILogger<HighCouncilMemberSyncService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly IScriptService _scriptService;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IElectionProvider _electionProvider;

    private const int MaxResultCount = 1000;

    public HighCouncilMemberSyncService(ILogger<HighCouncilMemberSyncService> logger, IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService, IScriptService scriptService, IElectionProvider electionProvider)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _scriptService = scriptService;
        _electionProvider = electionProvider;
        _graphQlProvider = graphQlProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        _logger.LogInformation("high council member sync start: chainId={0}, lastEndHeight={1}, newIndexHeight={2}",
            chainId, lastEndHeight, newIndexHeight);

        var daoIds = new HashSet<string>();
        var candidateElectedBlockHeight =
            await GetCandidateElectedDaoId(daoIds, chainId, lastEndHeight, newIndexHeight);
        var configChangedBlockHeight =
            await GetHighCouncilConfigChangedDaoId(daoIds, chainId, lastEndHeight, newIndexHeight);

        _logger.LogInformation(
            "high council member sync, changed daoIds={0}, chainId={1}, lastEndHeight={2}, newIndexHeight={3}",
            JsonConvert.SerializeObject(daoIds), chainId, lastEndHeight, newIndexHeight);

        foreach (var daoId in daoIds)
        {
            try
            {
                var addressList = await _scriptService.GetCurrentHCAsync(chainId, daoId);
                await _graphQlProvider.SetHighCouncilMembersAsync(chainId, daoId, addressList);
            }
            catch (Exception e)
            {
                _logger.LogError("high council member sync error, chainId={0}, daoId={1}", chainId, daoId);
            }
        }

        newIndexHeight = Math.Min(candidateElectedBlockHeight, configChangedBlockHeight);

        _logger.LogInformation("high council member sync end: newIndexHeight={0}", newIndexHeight);

        return newIndexHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.HighCouncilMemberSync;
    }

    private async Task<long> GetCandidateElectedDaoId(ISet<string> daoIds, string chainId, long lastEndHeight,
        long newIndexHeight)
    {
        try
        {
            var candidateElectedRecords = await _electionProvider.GetCandidateElectedRecordsAsync(
                input: new GetCandidateElectedRecordsInput
                {
                    ChainId = chainId,
                    SkipCount = 0,
                    MaxResultCount = MaxResultCount,
                    StartBlockHeight = lastEndHeight,
                    EndBlockHeight = newIndexHeight
                });

            var maxBlockHeight = lastEndHeight;
            if (!candidateElectedRecords.Items.IsNullOrEmpty())
            {
                foreach (var electedRecord in candidateElectedRecords.Items)
                {
                    daoIds.Add(electedRecord.DaoId);
                    maxBlockHeight = Math.Max(maxBlockHeight, electedRecord.BlockHeight);
                }
            }

            return !candidateElectedRecords.Items.IsNullOrEmpty() &&
                   candidateElectedRecords.Items.Count >= MaxResultCount
                ? Math.Min(maxBlockHeight, newIndexHeight)
                : newIndexHeight;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "query candidate elected daoId error: chainId={0}, lastEndHeight={1}, newIndexHeight={2}", chainId,
                lastEndHeight, newIndexHeight);
        }

        return newIndexHeight;
    }

    private async Task<long> GetHighCouncilConfigChangedDaoId(ISet<string> daoIds, string chainId, long lastEndHeight,
        long newIndexHeight)
    {
        try
        {
            var highCouncilConfig = await _electionProvider.GetHighCouncilConfigAsync(new GetHighCouncilConfigInput
            {
                ChainId = chainId,
                SkipCount = 0,
                MaxResultCount = MaxResultCount,
                StartBlockHeight = lastEndHeight,
                EndBlockHeight = newIndexHeight
            });

            long maxBlockHeight = lastEndHeight;
            if (!highCouncilConfig.Items.IsNullOrEmpty())
            {
                foreach (var electedRecord in highCouncilConfig.Items)
                {
                    daoIds.Add(electedRecord.DaoId);
                    maxBlockHeight = Math.Max(maxBlockHeight, electedRecord.BlockHeight);
                }
            }

            return !highCouncilConfig.Items.IsNullOrEmpty() &&
                   highCouncilConfig.Items.Count >= MaxResultCount
                ? Math.Min(maxBlockHeight, newIndexHeight)
                : newIndexHeight;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "query high council config changed daoId error: chainId={0}, lastEndHeight={1}, newIndexHeight={2}",
                chainId, lastEndHeight, newIndexHeight);
        }

        return newIndexHeight;
    }
}