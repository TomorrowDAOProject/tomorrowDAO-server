using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Election.Index;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;

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
                _logger.LogInformation("high council member count: daoId={0}, chaiId={1}, count={2}", daoId, chainId,
                    addressList.IsNullOrEmpty() ? 0 : addressList.Count);
                if (!addressList.IsNullOrEmpty())
                {
                    await _graphQlProvider.SetHighCouncilMembersAsync(chainId, daoId, addressList);
                }
                
                await UpdateHighCouncilManagedDaoIndexAsync(chainId, daoId,
                    new HashSet<string>(addressList ?? new List<string>()));
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

    private async Task UpdateHighCouncilManagedDaoIndexAsync(string chainId, string daoId, ISet<string> addressList)
    {
        _logger.LogInformation("Update HighCouncilManagedDAOIndex start... chain={0},daoId={1},current members={2}",
            chainId, daoId, JsonConvert.SerializeObject(addressList));
        try
        {
            var managedDaoIndices =
                await _electionProvider.GetHighCouncilManagedDaoIndexAsync(new GetHighCouncilMemberManagedDaoInput
                {
                    MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount,
                    SkipCount = 0,
                    ChainId = chainId,
                    DaoId = daoId
                });
            _logger.LogInformation("Update HighCouncilManangedDAOIndex, historical members:{0}",
                JsonConvert.SerializeObject(managedDaoIndices ?? new List<HighCouncilManagedDaoIndex>()));

            var deleteIndices = new List<HighCouncilManagedDaoIndex>();
            foreach (var managedDaoIndex in managedDaoIndices)
            {
                if (addressList.Contains(managedDaoIndex.MemberAddress))
                {
                    addressList.Remove(managedDaoIndex.MemberAddress);
                    continue;
                }
                deleteIndices.Add(managedDaoIndex);
            }

            var addIndices = addressList.Select(address => new HighCouncilManagedDaoIndex
                {
                    Id = GuidHelper.GenerateId(chainId, daoId, address),
                    MemberAddress = address,
                    DaoId = daoId,
                    ChainId = chainId,
                    CreateTime = DateTime.Now
                })
                .ToList();
            _logger.LogInformation("Update HighCouncilManagedDAOIndex, add members={0}, delete members={1}",
                JsonConvert.SerializeObject(addIndices), JsonConvert.SerializeObject(deleteIndices));
            if (!deleteIndices.IsNullOrEmpty())
            {
                await _electionProvider.DeleteHighCouncilManagedDaoIndexAsync(deleteIndices);
            }

            if (!addIndices.IsNullOrEmpty())
            {
                await _electionProvider.SaveOrUpdateHighCouncilManagedDaoIndexAsync(addIndices);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Update HighCouncilManagedDAOIndex error.");
        }
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