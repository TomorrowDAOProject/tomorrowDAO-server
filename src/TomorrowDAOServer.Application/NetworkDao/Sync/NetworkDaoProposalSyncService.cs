using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using Aetherlink.PriceServer.Common;
using FluentAssertions.Events;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using TomorrowDAO.Contracts.Vote;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Index;
using TomorrowDAOServer.NetworkDao.Migrator.Contract;
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
    private readonly INetworkDaoProposalProvider _networkDaoProposalProvider;
    private readonly IGraphQLProvider _graphQlProvider;

    private static readonly int MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount;
    private const int MaxVoteResultCount = 5000;
    private const int BatchSize = 100;

    private static readonly ISet<string> ContractDeployMethod = new HashSet<string>()
    {
        "ReleaseCodeCheckedContract", "RequestSideChainCreation", "ProposeContractCodeCheck", "ProposeNewContract",
        "ProposeUpdateContract", "DeployUserSmartContract", "UpdateUserSmartContract", "ReleaseApprovedContract"
    };

    private const string MethodCreateProposal = "CreateProposal";
    
    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .WithAElfTypesConverters()
        .IgnoreNullValue()
        .WithTimestampConverter()
        .WithGuardianTypeConverter()
        .Build();

    public NetworkDaoProposalSyncService(ILogger<NetworkDaoProposalSyncService> logger,
        INetworkDaoGraphQlDataProvider networkDaoGraphQlDataProvider,
        INetworkDaoEsDataProvider networkDaoEsDataProvider, IObjectMapper objectMapper,
        IContractProvider contractProvider, INetworkDaoContractProvider networkDaoContractProvider,
        IOptionsMonitor<MigratorOptions> migratorOptions, IExplorerProvider explorerProvider,
        INetworkDaoProposalProvider networkDaoProposalProvider, IGraphQLProvider graphQlProvider)
    {
        _logger = logger;
        _networkDaoGraphQlDataProvider = networkDaoGraphQlDataProvider;
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _objectMapper = objectMapper;
        _contractProvider = contractProvider;
        _networkDaoContractProvider = networkDaoContractProvider;
        _migratorOptions = migratorOptions;
        _explorerProvider = explorerProvider;
        _networkDaoProposalProvider = networkDaoProposalProvider;
        _graphQlProvider = graphQlProvider;
    }

    public async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        //TODO Test
        //lastEndHeight = 255831985; //255339490;
        //newIndexHeight = 255831987;

        if (newIndexHeight - lastEndHeight > 1000000)
        {
            newIndexHeight = lastEndHeight + 1000000;
        }
        var blockHeight = -1L;
        List<IndexerProposal> queryList;
        do
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("[NetworkDaoMigrator] proposal Sync, BlockHeight:{0}-{1}, SkipCount={2}",
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
            _logger.LogInformation("[NetworkDaoMigrator] proposal Sync, BlockHeight:{0}-{1}, SkipCount={2}, count={3}", lastEndHeight,
                newIndexHeight, skipCount,queryList?.Count);
            if (queryList.IsNullOrEmpty())
            {
                break;
            }
            
            blockHeight = Math.Max(blockHeight, queryList.Select(t => t.BlockHeight ?? -1L).Max()) + 1;

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

            //set proposal status
            await SetProposalStatus(chainId, proposalList);

            //builder proposal List index data
            var proposalListList = await BuildProposalListIndexAsync(chainId, proposalList);

            await _networkDaoEsDataProvider.BulkAddOrUpdateProposalIndexAsync(proposalList);
            await _networkDaoEsDataProvider.BulkAddOrUpdateProposalListIndexAsync(proposalListList);
            await _networkDaoEsDataProvider.BulkAddOrUpdateProposalVoteIndexAsync(voteIndices);

            skipCount += queryList.Count;

            stopwatch.Stop();
            _logger.LogInformation("[NetworkDaoMigrator]0.Sync proposal, count={0}, realCount={1}, duration={2}",
                queryList.Count, proposalList.Count, stopwatch.ElapsedMilliseconds);
        } while (queryList.Count == MaxResultCount);

        return blockHeight;
    }

    private async Task SetProposalStatus(string chainId, List<NetworkDaoProposalIndex> proposalList)
    {
        var now = DateTime.UtcNow;
        var filteredProposals = proposalList
            .Where(proposal => proposal.ExpiredTime > now && proposal.Status != NetworkDaoProposalStatusEnum.Released)
            .ToList();
        var orgAddresses = filteredProposals.Select(t => t.OrganizationAddress).ToList();
        var bpList = await _graphQlProvider.GetBPAsync(chainId);
        var (orgCount, orgIndices) = await _networkDaoEsDataProvider.GetOrgIndexAsync(new GetOrgListInput
        {
            MaxResultCount = MaxVoteResultCount,
            SkipCount = 0,
            ChainId = chainId,
            OrgAddresses = orgAddresses
        });
        var (memberCount, orgMemberIndices) = await _networkDaoEsDataProvider.GetOrgMemberListAsync(
            new GetOrgMemberListInput
            {
                MaxResultCount = MaxVoteResultCount,
                SkipCount = 0,
                ChainId = chainId,
                OrgAddresses = orgAddresses
            });
        var orgAddressToMembers = orgMemberIndices.GroupBy(t => t.OrgAddress)
            .ToDictionary(g => g.Key, g => g.ToList());

        orgIndices ??= new List<NetworkDaoOrgIndex>();
        var orgAddressToOrg = orgIndices.ToDictionary(t => t.OrgAddress, t => t);
        foreach (var proposalIndex in filteredProposals)
        {
            var orgIndex = orgAddressToOrg.GetValueOrDefault(proposalIndex.OrganizationAddress, null);
            if (orgIndex == null)
            {
                _logger.LogError(
                    "[NetworkDaoMigrator] Set Proposal Status, Not found Org. proposalId={0},OrgAddres={1}",
                    proposalIndex.ProposalId, proposalIndex.OrganizationAddress);
                continue;
            }

            switch (proposalIndex.OrgType)
            {
                case NetworkDaoOrgType.Parliament:
                {
                    proposalIndex.Status =
                        await _networkDaoProposalProvider.GetNetworkDaoProposalStatusAsync(chainId, proposalIndex,
                            orgIndex, bpList);
                    break;
                }
                case NetworkDaoOrgType.Association:
                {
                    var memberIndices = orgAddressToMembers.GetValueOrDefault(proposalIndex.OrganizationAddress,
                        new List<NetworkDaoOrgMemberIndex>());
                    var memberList = memberIndices.Select(t => t.Member).ToList();
                    proposalIndex.Status =
                        await _networkDaoProposalProvider.GetNetworkDaoProposalStatusAsync(chainId, proposalIndex,
                            orgIndex, memberList);
                    break;
                }
                default:
                {
                    proposalIndex.Status =
                        await _networkDaoProposalProvider.GetNetworkDaoProposalStatusAsync(chainId, proposalIndex,
                            orgIndex, new List<string>());
                    break;
                }
            }
        }
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
                _logger.LogInformation("[NetworkDaoMigrator] proposal={0}, vote count= 0", proposalIndex.ProposalId);
                continue;
            }
            var records = proposalVoteDictionary[proposalIndex.ProposalId];
            _logger.LogInformation("[NetworkDaoMigrator] proposal={0}, vote count= {1}", proposalIndex.ProposalId, records?.Count);
            if (records.IsNullOrEmpty())
            {
                continue;
            }

            proposalIndex.Approvals = 0;
            proposalIndex.Abstentions = 0;
            proposalIndex.Rejections = 0;

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
                    StartBlockHeight = 0,
                    EndBlockHeight = int.MaxValue,
                    SkipCount = 0,
                    MaxResultCount = MaxVoteResultCount,
                    ProposalIds = proposalIdBatch,
                    OrgType = NetworkDaoOrgType.All
                });
            if (voteRecords?.Data != null && voteRecords.Data.Count == MaxVoteResultCount)
            {
                throw new InvalidOperationException(
                    $"Result count is equal to the maximum allowed: {MaxVoteResultCount}. Possible incomplete data or pagination issue.");
            }

            voteRecordList.AddRange(voteRecords?.Data ?? new List<IndexerProposalVoteRecord>());
        }

        stopwatch.Stop();
        _logger.LogInformation("[NetworkDaoMigrator] proposals ={0} vote index, count={1}, duration={2}",
            JsonConvert.SerializeObject(proposalIds), voteRecordList.Count, stopwatch.ElapsedMilliseconds);

        return voteRecordList;
    }

    private async Task UpdateAndMergeLocalProposalDataAsync(string chainId, List<NetworkDaoProposalIndex> proposalList,
        long lastEndHeight)
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

                if (localProposalIndex.ExpiredTime != default)
                {
                    proposalIndex.ExpiredTime = localProposalIndex.ExpiredTime;
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
        if (releasedProposalIds.IsNullOrEmpty())
        {
            return new Dictionary<string, IndexerProposalReleased>();
        }

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
                _logger.LogInformation("[NetworkDaoMigrator] proposal={0}, processed proposal count {1}",
                    proposalIndex.ProposalId, count);
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("[NetworkDaoMigrator]proposal, 1.build proposal index, count={0}, duration={1}",
            queryList.Count,
            stopwatch.ElapsedMilliseconds);

        return proposalList;
    }

    private async Task<NetworkDaoProposalIndex> BuildProposalIndexAsync(string chainId,
        IndexerProposal indexerProposal)
    {
        var proposalIndex = _objectMapper.Map<IndexerProposal, NetworkDaoProposalIndex>(indexerProposal);
        //Id
        proposalIndex.Id = IdGeneratorHelper.GenerateId(chainId, proposalIndex.ProposalId);
        
        //ExpiredTime、ContractAddress、ContractMethod
        var explorerProposalInfo = await SetProposalExpiredTimeAsync(chainId, proposalIndex);
        if (explorerProposalInfo != null)
        {
            proposalIndex.ContractAddress = explorerProposalInfo.ContractAddress;
            proposalIndex.ContractMethod = explorerProposalInfo.ContractMethod;
            proposalIndex.Code = explorerProposalInfo.ContractParams;
        }
        else
        {
            var (contractAddress, contractMethod, code) = await GetProposalContractMethodNameAsync(indexerProposal);
            proposalIndex.ContractAddress = contractAddress;
            proposalIndex.ContractMethod = contractMethod;
            proposalIndex.Code = code;
        }

        //IsContractDeployed
        proposalIndex.IsContractDeployed = ContractDeployMethod.Contains(proposalIndex.ContractMethod);
        //Status
        if (proposalIndex.IsReleased)
        {
            proposalIndex.Status = NetworkDaoProposalStatusEnum.Released;
        }

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
        if (indexerProposal.TransactionInfo.IsAAForwardCall)
        {
            return new Tuple<string, string>(indexerProposal.TransactionInfo.RealTo ?? string.Empty,
                indexerProposal.TransactionInfo.RealMethodName ?? string.Empty);
        }

        return new Tuple<string, string>(indexerProposal.TransactionInfo.To ?? string.Empty,
            indexerProposal.TransactionInfo.MethodName ?? string.Empty);
    }

    private async Task<Tuple<string, string, string>> GetProposalContractMethodNameAsync(IndexerProposal indexerProposal)
    {
        _logger.LogInformation("[NetworkDaoMigrator] proposal={0}, transactionId={1}", indexerProposal.ProposalId,
            indexerProposal.TransactionInfo?.TransactionId);

        var contractAndMethodName = GetContractMethodName(indexerProposal);
        TransactionResultDto transactionResultDto = null;
        try
        {
            transactionResultDto = await _contractProvider.QueryTransactionResultAsync(
                indexerProposal.TransactionInfo.TransactionId,
                indexerProposal.ChainId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[NetworkDaoMigrator] proposal={0}, query transaction error.{1}", indexerProposal.ProposalId, e.Message);
            return new Tuple<string, string, string>(contractAndMethodName.Item1, contractAndMethodName.Item2, string.Empty);
        }
        
        if (transactionResultDto == null || transactionResultDto.TransactionId.IsNullOrWhiteSpace())
        {
            _logger.LogInformation("[NetworkDaoMigrator] proposal={0}, transaction not found", indexerProposal.ProposalId);
            return new Tuple<string, string, string>(contractAndMethodName.Item1, contractAndMethodName.Item2, string.Empty);
        }

        try
        {
            // var voteEventLog = transactionResultDto.Logs.First(l => l.Name == "CodeCheckRequired");
            // var voteEvent = LogEventDeserializationHelper.DeserializeLogEvent<CodeCheckRequired>(voteEventLog);
            if (contractAndMethodName.Item2 == MethodCreateProposal)
            {
                var param = transactionResultDto.Transaction.Params;
                _logger.LogInformation("[NetworkDaoMigrator] proposal={0}, param={1}", indexerProposal.ProposalId, param);
                if (indexerProposal.TransactionInfo.IsAAForwardCall)
                {
                    var forwardCallParam =
                        JsonConvert.DeserializeObject<ForwardCallParam>(param);
                    if (forwardCallParam.Args.IsNullOrWhiteSpace())
                    {
                        return new Tuple<string, string, string>(contractAndMethodName.Item1, contractAndMethodName.Item2, string.Empty);
                    }
                    //unpack packed input
                    var createProposalInput = AElf.Standards.ACS3.CreateProposalInput.Parser.ParseFrom(
                        ByteString.FromBase64(forwardCallParam.Args));
                    param = JsonConvert.SerializeObject(createProposalInput, DefaultJsonSettings);
                }
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(param);
                var toAddress = dictionary.GetValueOrDefault("toAddress", string.Empty).ToString();
                var contractMethodName = dictionary.GetValueOrDefault("contractMethodName", string.Empty).ToString();
                var code = dictionary.GetValueOrDefault("params", string.Empty).ToString();
                // if (toAddress == "pykr77ft9UUKJZLVq15wCH8PinBSjVRQ12sD1Ayq92mKFsJ1i" &&
                //     contractMethodName is "DeploySmartContract" or "DeploySystemSmartContract" or "UpdateSmartContract")
                // {
                //     return new Tuple<string, string>(toAddress, contractMethodName);
                // }
                return new Tuple<string, string, string>(toAddress, contractMethodName, code);
            }
        
            return new Tuple<string, string, string>(contractAndMethodName.Item1, contractAndMethodName.Item2, string.Empty);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[NetworkDaoMigrator] parse param error.{0}", e.Message);
            return new Tuple<string, string, string>(contractAndMethodName.Item1, contractAndMethodName.Item2, string.Empty);
        }
    }

    private async Task<ExplorerProposalInfo> SetProposalExpiredTimeAsync(string chainId, NetworkDaoProposalIndex proposalIndex)
    {
        try
        {
            var proposalOutput =
                await _networkDaoContractProvider.GetProposalAsync(chainId, proposalIndex.OrgType,
                    proposalIndex.ProposalId);
            if (proposalOutput != null && proposalOutput.ProposalId != null &&
                proposalOutput.ProposalId.ToHex() == proposalIndex.ProposalId)
            {
                if (proposalOutput.ExpiredTime != null)
                {
                    proposalIndex.ExpiredTime = proposalOutput.ExpiredTime.ToDateTime();
                }
                else
                {
                    _logger.LogInformation("[NetworkDaoMigrator] proposalId={0}, expiredtime is default {1}",
                        proposalIndex.ProposalId, JsonConvert.SerializeObject(proposalOutput));
                }

                return null;
            }
            else
            {
                _logger.LogInformation("[NetworkDaoMigrator] proposalId={0}, proposal not found.",
                    proposalIndex.ProposalId);
            }

            if (_migratorOptions.CurrentValue.QueryExplorerProposal)
            {
                var explorerResp = await _explorerProvider.GetProposalInfoAsync(chainId,
                    new ExplorerProposalInfoRequest()
                    {
                        ProposalId = proposalIndex.ProposalId
                    });

                if (explorerResp == null || explorerResp.Proposal == null || explorerResp.Proposal.ProposalId != proposalIndex.ProposalId)
                {
                    return null;
                }

                var explorerProposal = explorerResp.Proposal;
                proposalIndex.ExpiredTime = explorerProposal!.ExpiredTime;
                
                return explorerProposal;
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

        return null;
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