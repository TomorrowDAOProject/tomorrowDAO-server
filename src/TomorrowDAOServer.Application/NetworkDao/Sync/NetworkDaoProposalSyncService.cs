using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Aetherlink.PriceServer.Common;
using FluentAssertions.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Index;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Migrator.GraphQL;
using TomorrowDAOServer.NetworkDao.Options;
using TomorrowDAOServer.NetworkDao.Provider;
using TomorrowDAOServer.Providers;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using ProposalType = TomorrowDAOServer.Common.Enum.ProposalType;

namespace TomorrowDAOServer.NetworkDao.Sync;

public interface INetworkDaoProposalSyncService
{
    Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight);
}

public class NetworkDaoProposalSyncService : INetworkDaoProposalSyncService, ISingletonDependency
{
    private readonly ILogger<NetworkDaoProposalSyncService> _logger;
    private readonly INetworkDaoGraphQlDataProvider _networkDaoGraphQlDataProvider;
    private readonly INetworkDaoEsDataProvider _networkDaoEsDataProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IContractProvider _contractProvider;
    private readonly INetworkDaoContractProvider _networkDaoContractProvider;
    private readonly IOptionsMonitor<MigratorOptions> _migratorOptions;
    private readonly IExplorerProvider _explorerProvider;

    private static readonly int MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount;
    private const int MaxVoteResultCount = 5000;
    private const int BatchSize = 100;

    private static readonly ISet<string> ContractDeployMethod = new HashSet<string>()
    {
        "ReleaseCodeCheckedContract", "RequestSideChainCreation", "ProposeContractCodeCheck", "ProposeNewContract",
        "ProposeUpdateContract", "DeployUserSmartContract", "UpdateUserSmartContract", "ReleaseApprovedContract"
    };

    public NetworkDaoProposalSyncService(ILogger<NetworkDaoProposalSyncService> logger,
        INetworkDaoGraphQlDataProvider networkDaoGraphQlDataProvider,
        INetworkDaoEsDataProvider networkDaoEsDataProvider, IObjectMapper objectMapper,
        IContractProvider contractProvider, INetworkDaoContractProvider networkDaoContractProvider,
        IOptionsMonitor<MigratorOptions> migratorOptions, IExplorerProvider explorerProvider)
    {
        _logger = logger;
        _networkDaoGraphQlDataProvider = networkDaoGraphQlDataProvider;
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _objectMapper = objectMapper;
        _contractProvider = contractProvider;
        _networkDaoContractProvider = networkDaoContractProvider;
        _migratorOptions = migratorOptions;
        _explorerProvider = explorerProvider;
    }

    public async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        List<IndexerProposal> queryList;
        do
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("[NetworkDaoMigrator]Sync proposal, BlockHeight:{0}-{1}, SkipCount={2}",
                lastEndHeight,
                newIndexHeight, skipCount);
            //query the proposal data of changed
            queryList = (await _networkDaoGraphQlDataProvider.GetNetworkDaoProposalIndexAsync(
                new GetProposalIndexInput
                {
                    ChainId = chainId,
                    OrgType = NetworkDaoOrgType.All,
                    StartBlockHeight = lastEndHeight,
                    EndBlockHeight = newIndexHeight,
                    SkipCount = skipCount,
                    MaxResultCount = MaxResultCount,
                    ContractNames = _migratorOptions.CurrentValue.FilterGraphQLToAddresses,
                    MethodNames = _migratorOptions.CurrentValue.FilterGraphQLMethodNames
                })).Data;
            _logger.LogInformation("[NetworkDaoMigrator]Sync proposal,count:{count}", queryList?.Count);
            if (queryList.IsNullOrEmpty())
            {
                break;
            }

            //build Proposal index data
            var proposalList = await BuildProposalIndexAsync(chainId, queryList);

            //query and merge local data（release、vote）
            await UpdateAndMergeLocalProposalDataAsync(chainId, proposalList, lastEndHeight);

            //query the vote data of changed
            var voteRecords = await BuildProposalVoteIndexAsync(chainId, lastEndHeight, newIndexHeight, proposalList);
            var voteIndices =
                _objectMapper.Map<List<IndexerProposalVoteRecord>, List<NetworkDaoProposalVoteIndex>>(voteRecords);

            //update voting info of proposal
            UpdateProposalVoteInfo(proposalList, voteRecords);

            //builder proposal List index data
            var proposalListList = await BuildProposalListIndexAsync(chainId, proposalList);

            await _networkDaoEsDataProvider.BulkAddOrUpdateProposalIndexAsync(proposalList);
            await _networkDaoEsDataProvider.BulkAddOrUpdateProposalListIndexAsync(proposalListList);
            await _networkDaoEsDataProvider.BulkAddOrUpdateProposalVoteIndexAsync(voteIndices);

            skipCount += queryList.Count;

            stopwatch.Stop();
            _logger.LogInformation("[NetworkDaoMigrator]0.Sync proposal, count={0}, realCount={1}, duration={2}", 
                queryList.Count, proposalList.Count,stopwatch.ElapsedMilliseconds);
        } while (queryList.Count == MaxResultCount);

        return newIndexHeight;
    }

    private async Task<List<NetworkDaoProposalListIndex>> BuildProposalListIndexAsync(string chainId,
        List<NetworkDaoProposalIndex> proposalList)
    {
        var proposalListList =
            _objectMapper.Map<List<NetworkDaoProposalIndex>, List<NetworkDaoProposalListIndex>>(proposalList);
        return proposalListList;
    }

    private void UpdateProposalVoteInfo(List<NetworkDaoProposalIndex> proposalList,
        List<IndexerProposalVoteRecord> voteRecords)
    {
        if (voteRecords.IsNullOrEmpty())
        {
            return;
        }

        var proposalVoteDictionary = voteRecords
            .GroupBy(record => record.ProposalId)
            .ToDictionary(group => group.Key, group => group.ToList());
        foreach (var proposalIndex in proposalList)
        {
            if (!proposalVoteDictionary.ContainsKey(proposalIndex.ProposalId))
            {
                continue;
            }

            var records = proposalVoteDictionary[proposalIndex.ProposalId];
            if (records.IsNullOrEmpty())
            {
                continue;
            }

            foreach (var voteRecord in records)
            {
                switch (voteRecord.ReceiptType)
                {
                    case NetworkDaoReceiptTypeEnum.Approve:
                        proposalIndex.Approvals += voteRecord.Amount;
                        break;
                    case NetworkDaoReceiptTypeEnum.Abstain:
                        proposalIndex.Abstentions += voteRecord.Amount;
                        break;
                    case NetworkDaoReceiptTypeEnum.Reject:
                        proposalIndex.Rejections += voteRecord.Amount;
                        break;
                }
            }
        }
    }

    private async Task<List<IndexerProposalVoteRecord>> BuildProposalVoteIndexAsync(string chainId, long lastEndHeight,
        long newIndexHeight, List<NetworkDaoProposalIndex> proposalList)
    {
        var stopwatch = Stopwatch.StartNew();
        var voteRecordList = new List<IndexerProposalVoteRecord>();
        var proposalIds = proposalList.Select(t => t.ProposalId).ToList();
        var proposalIdBatches = proposalIds
            .Select((id, index) => new { id, index })
            .GroupBy(x => x.index / BatchSize)
            .Select(group => group.Select(x => x.id).ToList())
            .ToList();

        foreach (var proposalIdBatch in proposalIdBatches)
        {
            var voteRecords = await _networkDaoGraphQlDataProvider.GetNetworkDaoProposalVoteRecordAsync(
                new GetProposalVoteRecordIndexInput
                {
                    ChainId = chainId,
                    StartBlockHeight = lastEndHeight,
                    EndBlockHeight = newIndexHeight,
                    SkipCount = 0,
                    MaxResultCount = MaxVoteResultCount,
                    ProposalIds = proposalIdBatch,
                });
            if (voteRecords?.Data != null && voteRecords.Data.Count == MaxVoteResultCount)
            {
                throw new InvalidOperationException(
                    $"Result count is equal to the maximum allowed: {MaxVoteResultCount}. Possible incomplete data or pagination issue.");
            }

            voteRecordList.AddRange(voteRecords?.Data ?? new List<IndexerProposalVoteRecord>());
        }

        stopwatch.Stop();
        _logger.LogInformation("[NetworkDaoMigrator]3.build proposal vote index, count={0}, duration={1}",
            voteRecordList.Count, stopwatch.ElapsedMilliseconds);

        return voteRecordList;
    }

    private async Task UpdateAndMergeLocalProposalDataAsync(string chainId, List<NetworkDaoProposalIndex> proposalList, long lastEndHeight)
    {
        if (proposalList.IsNullOrEmpty())
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        var proposalIds = proposalList.Select(t => t.ProposalId).ToList();
        var releasedProposalIds = proposalList.Where(t => t.IsReleased)
            .Select(t => t.ProposalId).ToList();

        var localProposalDic = await GetLocalProposalDicAsync(chainId, proposalIds);
        var proposalReleasedRecords =
            await GetProposalReleasedRecordsAsync(chainId, releasedProposalIds);

        foreach (var proposalIndex in proposalList)
        {
            if (localProposalDic.ContainsKey(proposalIndex.ProposalId))
            {
                var localProposalIndex = localProposalDic[proposalIndex.ProposalId];
                //Update voting info
                //Initialize synchronization, no need to update the number of votes
                //Determine whether it is initialization by lastEndHeight
                if (lastEndHeight < 1000)
                {
                    proposalIndex.Approvals = localProposalIndex.Approvals;
                    proposalIndex.Abstentions = localProposalIndex.Abstentions;
                    proposalIndex.Rejections = localProposalIndex.Rejections;
                }

                proposalIndex.BlockHash = localProposalIndex.BlockHash;
                proposalIndex.BlockHeight = localProposalIndex.BlockHeight;
                proposalIndex.BlockTime = localProposalIndex.BlockTime;
                proposalIndex.PreviousBlockHash = localProposalIndex.PreviousBlockHash;
            }

            if (proposalReleasedRecords.ContainsKey(proposalIndex.ProposalId))
            {
                //update release info
                var proposalReleased = proposalReleasedRecords[proposalIndex.ProposalId];
                proposalIndex.ReleasedTxId = proposalReleased.TransactionInfo.TransactionId ?? string.Empty;
                proposalIndex.ReleasedTime = proposalReleased.BlockTime ?? new DateTime();
                proposalIndex.ReleasedBlockHeight = proposalReleased.BlockHeight?.ToString() ?? string.Empty;
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("[NetworkDaoMigrator]2.update and merge proposal index, count={0}, duration={1}",
            localProposalDic.Count, stopwatch.ElapsedMilliseconds);
    }

    private async Task<Dictionary<string, IndexerProposalReleased>> GetProposalReleasedRecordsAsync(string chainId,
        List<string> releasedProposalIds)
    {
        var pageResultDto = await _networkDaoGraphQlDataProvider.GetNetworkDaoProposalReleasedIndexAsync(
            new GetProposalReleasedIndexInput
            {
                ChainId = chainId,
                SkipCount = 0,
                MaxResultCount = MaxResultCount,
                ProposalIds = releasedProposalIds
            });
        if (pageResultDto?.Data == null || pageResultDto.Data.IsNullOrEmpty())
        {
            return new Dictionary<string, IndexerProposalReleased>();
        }

        return pageResultDto.Data.ToDictionary(t => t.ProposalId);
    }

    private async Task<List<NetworkDaoProposalIndex>> BuildProposalIndexAsync(string chainId,
        List<IndexerProposal> queryList)
    {
        var stopwatch = Stopwatch.StartNew();
        var proposalList = new List<NetworkDaoProposalIndex>();
        var count = 0;
        foreach (var indexerProposal in queryList!)
        {
            //Filter Proposal
            var (contractAddress, contractMethod) = GetContractMethodName(indexerProposal);
            if (_migratorOptions.CurrentValue.FilterContractMethods.Contains($"{contractAddress}.{contractMethod}")
                || _migratorOptions.CurrentValue.FilterMethods.Contains(contractMethod))
            {
                continue;
            }
            
            var proposalIndex = await BuildProposalIndexAsync(chainId, indexerProposal);
            proposalList.Add(proposalIndex);
            if (++count % 10 == 0)
            {
                _logger.LogInformation("[NetworkDaoMigrator] processed proposal count {0}", count);
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("[NetworkDaoMigrator]1.build proposal index, count={0}, duration={1}", queryList.Count,
            stopwatch.ElapsedMilliseconds);

        return proposalList;
    }

    private async Task<NetworkDaoProposalIndex> BuildProposalIndexAsync(string chainId,
        IndexerProposal indexerProposal)
    {
        var proposalIndex = _objectMapper.Map<IndexerProposal, NetworkDaoProposalIndex>(indexerProposal);
        //Id
        proposalIndex.Id = IdGeneratorHelper.GenerateId(chainId, proposalIndex.ProposalId);
        //ContractAddress、ContractMethod
        var (contractAddress, contractMethod) = GetContractMethodName(indexerProposal);
        proposalIndex.ContractAddress = contractAddress;
        proposalIndex.ContractMethod = contractMethod;

        //IsContractDeployed
        proposalIndex.IsContractDeployed = ContractDeployMethod.Contains(proposalIndex.ContractMethod);
        //Status
        if (proposalIndex.IsReleased)
        {
            proposalIndex.Status = NetworkDaoProposalStatusEnum.Released;
        }

        //ExpiredTime
        await SetProposalExpiredTimeAsync(chainId, proposalIndex);

        //Proposer
        if (proposalIndex.TransactionInfo.IsAAForwardCall)
        {
            var caHash = proposalIndex.TransactionInfo.CAHash;
            var caContractAddress = proposalIndex.TransactionInfo.PortKeyContract;

            var getHolderInfoOutput =
                await _networkDaoContractProvider.GetHolderInfoAsync(chainId, caContractAddress, caHash);
            proposalIndex.Proposer = getHolderInfoOutput.CaAddress?.ToBase58() ?? string.Empty;
        }
        else
        {
            proposalIndex.Proposer = proposalIndex.TransactionInfo.From ?? string.Empty;
        }

        return proposalIndex;
    }

    private static Tuple<string, string> GetContractMethodName(IndexerProposal indexerProposal)
    {
        NetworkDaoProposalIndex proposalIndex;
        if (indexerProposal.TransactionInfo.IsAAForwardCall)
        {
            return new Tuple<string, string>(indexerProposal.TransactionInfo.RealTo ?? string.Empty,
                indexerProposal.TransactionInfo.RealMethodName ?? string.Empty);
        }
        
        return new Tuple<string, string>(indexerProposal.TransactionInfo.To ?? string.Empty,
            indexerProposal.TransactionInfo.MethodName ?? string.Empty);
    }

    private async Task SetProposalExpiredTimeAsync(string chainId, NetworkDaoProposalIndex proposalIndex)
    {
        try
        {
            var proposalOutput =
                await _networkDaoContractProvider.GetProposalAsync(chainId, proposalIndex.OrgType,
                    proposalIndex.ProposalId);
            if (proposalOutput != null && proposalOutput.ProposalId != null && proposalOutput.ProposalId != Hash.Empty)
            {
                if (proposalOutput.ExpiredTime != null)
                {
                    proposalIndex.ExpiredTime = proposalOutput.ExpiredTime.ToDateTime();
                }
                return;
            }

            if (_migratorOptions.CurrentValue.QueryExplorerProposal)
            {
                var explorerResp = await _explorerProvider.GetProposalPagerAsync(chainId,
                    new ExplorerProposalListRequest
                    {
                        ProposalType = proposalIndex.OrgType.ToString(),
                        Search = proposalIndex.ProposalId
                    });

                if (explorerResp == null || explorerResp.List.IsNullOrEmpty())
                {
                    return;
                }

                var explorerProposalResult = explorerResp.List.FirstOrDefault();
                proposalIndex.ExpiredTime = explorerProposalResult!.ExpiredTime;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query Proposal ExpiredTime fail. proposalId={0}, orgType={1}",
                proposalIndex.ProposalId, proposalIndex.OrgType.ToString());
            if (proposalIndex.SaveTime != null)
            {
                proposalIndex.ExpiredTime = proposalIndex.SaveTime.AddMinutes(10);
            }
        }
    }

    private async Task<Dictionary<string, NetworkDaoProposalIndex>> GetLocalProposalDicAsync(string chainId,
        List<string> proposalIds)
    {
        var (totalCount, localProposalList) = await _networkDaoEsDataProvider.GetProposalListAsync(
            new GetProposalListInput
            {
                MaxResultCount = MaxResultCount,
                SkipCount = 0,
                ChainId = chainId,
                ProposalIds = proposalIds
            });
        if (localProposalList.IsNullOrEmpty())
        {
            return new Dictionary<string, NetworkDaoProposalIndex>();
        }

        return localProposalList.ToDictionary(t => t.ProposalId);
    }
}