using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Election;
using AElf.ExceptionHandler;
using Amazon.Runtime.Internal;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Dto;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;
using AddressHelper = TomorrowDAOServer.Common.AddressHelper;
using ProposalType = TomorrowDAOServer.Common.Enum.ProposalType;

namespace TomorrowDAOServer.NetworkDao;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class NetworkDaoProposalService : TomorrowDAOServerAppService, INetworkDaoProposalService
{
    private readonly ILogger<NetworkDaoProposalService> _logger;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IOptionsMonitor<NetworkDaoOptions> _networkDaoOptions;
    private readonly IDistributedCache<string> _currentTermMiningRewardCache;
    private readonly INetworkDaoProposalProvider _networkDaoProposalProvider;
    private readonly INetworkDaoEsDataProvider _networkDaoEsDataProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INetworkDaoOrgService _networkDaoOrgService;
    private readonly INetworkDaoVoteService _networkDaoVoteService;

    private const int DefaultMaxResultCount = 1000;


    // pubKey => CandidateDetail.Hex
    private readonly IDistributedCache<Dictionary<string, string>> _candidateDetailCache;

    // two-layer cache
    private readonly IDistributedCache<Dictionary<string, ExplorerProposalResult>> _proposalResultCache;
    private readonly IDistributedCache<Dictionary<string, ExplorerProposalResult>> _proposalResultCacheBottom;

    public NetworkDaoProposalService(IExplorerProvider explorerProvider, ILogger<NetworkDaoProposalService> logger,
        IContractProvider contractProvider, IDistributedCache<string> currentTermMiningRewardCache,
        IOptionsMonitor<NetworkDaoOptions> networkDaoOptions,
        IDistributedCache<Dictionary<string, string>> candidateDetailCache, IObjectMapper objectMapper,
        IDistributedCache<Dictionary<string, ExplorerProposalResult>> proposalResultCache,
        IDistributedCache<Dictionary<string, ExplorerProposalResult>> proposalResultCacheBottom,
        INetworkDaoProposalProvider networkDaoProposalProvider, INetworkDaoEsDataProvider networkDaoEsDataProvider,
        IGraphQLProvider graphQlProvider, INetworkDaoOrgService networkDaoOrgService,
        INetworkDaoVoteService networkDaoVoteService)
    {
        _explorerProvider = explorerProvider;
        _logger = logger;
        _contractProvider = contractProvider;
        _currentTermMiningRewardCache = currentTermMiningRewardCache;
        _networkDaoOptions = networkDaoOptions;
        _candidateDetailCache = candidateDetailCache;
        _objectMapper = objectMapper;
        _proposalResultCache = proposalResultCache;
        _proposalResultCacheBottom = proposalResultCacheBottom;
        _networkDaoProposalProvider = networkDaoProposalProvider;
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _graphQlProvider = graphQlProvider;
        _networkDaoOrgService = networkDaoOrgService;
        _networkDaoVoteService = networkDaoVoteService;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndThrow),
        Message = "Failed to query the proposal list.",
        LogTargets = new[] { "request" })]
    public virtual async Task<ExplorerProposalResponse> GetProposalListAsync(ProposalListRequest request)
    {
        var explorerResp = await _explorerProvider.GetProposalPagerAsync(request.ChainId,
            new ExplorerProposalListRequest
            {
                PageSize = request.PageSize,
                PageNum = request.PageNum,
                Status = request.Status,
                IsContract = request.IsContract,
                ProposalType = request.ProposalType,
                Search = request.Search,
                Address = request.Address
            });

        if (explorerResp == null || explorerResp.List.IsNullOrEmpty())
        {
            return explorerResp;
        }

        var proposalIds = explorerResp.List.Select(item => item.ProposalId).ToHashSet().ToList();
        var proposalDictionary = await GetNetworkDaoProposalsDictionaryAsync(request.ChainId, proposalIds);
        //var items = _objectMapper.Map<List<ExplorerProposalResult>, List<ProposalListResponse>>(explorerResp.List);

        foreach (var item in explorerResp.List)
        {
            if (!proposalDictionary.ContainsKey(item.ProposalId))
            {
                continue;
            }

            var networkDaoProposalDto = proposalDictionary[item.ProposalId];
            item.Title = networkDaoProposalDto.Title;
            item.Description = networkDaoProposalDto.Description;
        }

        return explorerResp;
    }

    public async Task<NetworkDaoProposalDto> GetProposalInfoAsync(ProposalInfoRequest request)
    {
        if (request.ChainId.IsNullOrWhiteSpace() || request.ProposalId.IsNullOrWhiteSpace())
        {
            return new NetworkDaoProposalDto();
        }

        var proposals =
            await GetNetworkDaoProposalsAsync(request.ChainId, new List<string>() { request.ProposalId });

        return proposals.IsNullOrEmpty() ? new NetworkDaoProposalDto() : proposals[0];
    }

    public async Task<GetProposalListPageResult> GetProposalListAsync(GetProposalListInput input)
    {
        try
        {
            var address = input.Address;
            input.Address = string.Empty;
            var (totalCount, proposalListList) = await _networkDaoEsDataProvider.GetProposalListListAsync(input);

            var orgAddresses = proposalListList.Select(t => t.OrganizationAddress).ToList();
            var orgAddressToOrg = await _networkDaoOrgService.GetOrgDictionaryAsync(input.ChainId, orgAddresses);
            var orgAddressToProposerWhiteList =
                await _networkDaoOrgService.GetOrgProposerWhiteListDictionaryAsync(input.ChainId, input.ProposalType,
                    orgAddresses);
            var orgAddressToMember =
                await _networkDaoOrgService.GetOrgMemberDictionaryAsync(input.ChainId, input.ProposalType,
                    orgAddresses);

            var proposalIds = proposalListList.Select(t => t.ProposalId).ToList();
            var proposalIdToVotes =
                await _networkDaoVoteService.GetPersonVotedDictionaryAsync(input.ChainId, address, proposalIds);


            var getProposalListResultDtos = new List<GetProposalListResultDto>();
            if (!proposalListList.IsNullOrEmpty())
            {
                foreach (var proposalListIndex in proposalListList)
                {
                    var proposalListResultDto =
                        _objectMapper.Map<NetworkDaoProposalListIndex, GetProposalListResultDto>(proposalListIndex);

                    var orgIndex =
                        orgAddressToOrg.GetValueOrDefault(proposalListResultDto.OrgAddress, new NetworkDaoOrgIndex());
                    var orgMemberList =
                        orgAddressToMember.GetValueOrDefault(proposalListResultDto.OrgAddress, new List<string>());
                    var orgProposerList = orgAddressToProposerWhiteList.GetValueOrDefault(proposalListResultDto.OrgAddress, new List<string>());
                    var hasVoted = proposalIdToVotes.ContainsKey(proposalListResultDto.ProposalId);
                    var leftInfoDto = new GetProposalListResultDto.LeftInfoDto();
                    leftInfoDto.OrganizationAddress = orgIndex.OrgAddress;
                    proposalListResultDto.Abstentions = proposalListIndex.Abstentions;
                    proposalListResultDto.Approvals = proposalListIndex.Approvals;
                    proposalListResultDto.Rejections = proposalListIndex.Rejections;
                    proposalListResultDto.CanVote = !hasVoted && await CanVote(input.ChainId, address, proposalListIndex.Status, proposalListIndex.ExpiredTime, orgIndex, orgMemberList);
                    proposalListResultDto.LeftInfo = leftInfoDto;
                    proposalListResultDto.OrganizationInfo = _networkDaoOrgService.ConvertToOrgDto(orgIndex, orgMemberList, orgProposerList);
                    proposalListResultDto.TxId = string.Empty;
                    proposalListResultDto.UpdatedAt = DateTime.Now;
                    proposalListResultDto.VotedStatus = hasVoted ? proposalIdToVotes[proposalListResultDto.ProposalId].ReceiptType.ToString(): "none";

                    getProposalListResultDtos.Add(proposalListResultDto);
                }
            }

            var bpList = await _graphQlProvider.GetBPAsync(input.ChainId);
            var count = bpList.IsNullOrEmpty() ? 0 : bpList.Count;

            return new GetProposalListPageResult
            {
                TotalCount = totalCount,
                Items = getProposalListResultDtos,
                BpCount = count
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get proposal list error. request={0}", JsonConvert.SerializeObject(input));
            throw new UserFriendlyException("Failed to query the proposal list. {0}", e.Message);
        }
    }

    public async Task<Dictionary<string, Dictionary<NetworkDaoReceiptTypeEnum, long>>> GetProposalVotedAmountAsync(
        string chainId, List<NetworkDaoProposalListIndex> proposalListList)
    {
        if (proposalListList.IsNullOrEmpty())
        {
            return new AutoConstructedDictionary<string, Dictionary<NetworkDaoReceiptTypeEnum, long>>();
        }

        var proposalIds = proposalListList.Select(t => t.ProposalId).ToList();
        var (votedCount, voteIndices) = await _networkDaoEsDataProvider.GetProposalVotedListAsync(
            new GetVotedListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount,
                SkipCount = 0,
                ChainId = chainId,
                ProposalIds = proposalIds
            });
        var votedDictionary = new Dictionary<string, Dictionary<NetworkDaoReceiptTypeEnum, long>>();
        if (!voteIndices.IsNullOrEmpty())
        {
            foreach (var voteIndex in voteIndices)
            {
                var proposalVotedDic = votedDictionary.GetValueOrDefault(voteIndex.ProposalId,
                    new Dictionary<NetworkDaoReceiptTypeEnum, long>());

                var voteCount = proposalVotedDic.GetValueOrDefault(voteIndex.ReceiptType, 0);
                voteCount += voteIndex.Amount;

                proposalVotedDic[voteIndex.ReceiptType] = voteCount;
                votedDictionary[voteIndex.ProposalId] = proposalVotedDic;
            }
        }

        return votedDictionary;
    }

    public async Task<GetProposalInfoResultDto> GetProposalInfoAsync(GetProposalInfoInput input)
    {
        try
        {
            var address = input.Address;
            input.Address = string.Empty;
            var proposalIndex = await _networkDaoEsDataProvider.GetProposalIndexAsync(input);

            var orgAddresses = new List<string>() { proposalIndex.OrganizationAddress };
            var orgAddressToOrg = await _networkDaoOrgService.GetOrgDictionaryAsync(input.ChainId, orgAddresses);
            var orgAddressToProposerWhiteList =
                await _networkDaoOrgService.GetOrgProposerWhiteListDictionaryAsync(input.ChainId, proposalIndex.OrgType,
                    orgAddresses);
            var orgAddressToMember =
                await _networkDaoOrgService.GetOrgMemberDictionaryAsync(input.ChainId, proposalIndex.OrgType,
                    orgAddresses);

            var proposalIds = new List<string>() { proposalIndex.ProposalId };
            var proposalIdToVotes =
                await _networkDaoVoteService.GetPersonVotedDictionaryAsync(input.ChainId, address, proposalIds);
            

            var proposalListResultDto =
                _objectMapper.Map<NetworkDaoProposalIndex, GetProposalListResultDto>(proposalIndex);
            var orgIndex =
                orgAddressToOrg.GetValueOrDefault(proposalListResultDto.OrgAddress, new NetworkDaoOrgIndex());
            var orgMemberList =
                orgAddressToMember.GetValueOrDefault(proposalListResultDto.OrgAddress, new List<string>());
            var orgProposerList = orgAddressToProposerWhiteList.GetValueOrDefault(proposalListResultDto.OrgAddress, new List<string>());
            var hasVoted = proposalIdToVotes.ContainsKey(proposalListResultDto.ProposalId);
            var leftInfoDto = new GetProposalListResultDto.LeftInfoDto();
            leftInfoDto.OrganizationAddress = orgIndex.OrgAddress;
            
            proposalListResultDto.Abstentions = proposalIndex.Abstentions;
            proposalListResultDto.Approvals = proposalIndex.Approvals;
            proposalListResultDto.Rejections = proposalIndex.Rejections;
            proposalListResultDto.CanVote = !hasVoted && await CanVote(input.ChainId, address, proposalIndex.Status, proposalIndex.ExpiredTime, orgIndex, orgMemberList);;
            proposalListResultDto.LeftInfo = leftInfoDto;
            //proposalListResultDto.OrganizationInfo = new NetworkDaoOrgDto();
            proposalListResultDto.TxId = string.Empty;
            proposalListResultDto.UpdatedAt = DateTime.Now;
            proposalListResultDto.VotedStatus = hasVoted ? proposalIdToVotes[proposalListResultDto.ProposalId].ReceiptType.ToString(): "none";

            var bpList = await _graphQlProvider.GetBPAsync(input.ChainId);

            return new GetProposalInfoResultDto
            {
                Proposal = proposalListResultDto,
                BpList = bpList,
                Organization = _networkDaoOrgService.ConvertToOrgDto(orgIndex, orgMemberList, orgProposerList),
                ParliamentProposerList = new List<string>(),
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get proposal info error. request={0}", JsonConvert.SerializeObject(input));
            throw new UserFriendlyException("Failed to query the proposal info. {0}", e.Message);
        }
    }

    public async Task<GetAppliedListPagedResult> GetAppliedProposalListAsync(GetAppliedListInput input)
    {
        var (totalCount, networkDaoProposalIndices) = await _networkDaoEsDataProvider.GetProposalListAsync(
            new GetProposalListInput
            {
                MaxResultCount = input.MaxResultCount,
                SkipCount = input.SkipCount,
                ChainId = input.ChainId,
                Proposer = input.Address,
                ProposalType = input.ProposalType, 
                Search = input.Search
            });
        if (networkDaoProposalIndices.IsNullOrEmpty())
        {
            return new GetAppliedListPagedResult
            {
                Items = new List<GetAppliedListResultDto>(),
                TotalCount = totalCount
            };
        }

        var resultDtos =
            _objectMapper.Map<List<NetworkDaoProposalIndex>, List<GetAppliedListResultDto>>(networkDaoProposalIndices);
        return new GetAppliedListPagedResult
        {
            Items = resultDtos,
            TotalCount = totalCount
        };
    }
    
    private async Task<bool> CanVote(string chainId, string address,
        NetworkDaoProposalStatusEnum status,
        DateTime expiredTime,
        NetworkDaoOrgIndex orgIndex, List<string> orgMemberList)
    {
        if (address.IsNullOrWhiteSpace())
        {
            return false;
        }

        if (orgIndex.OrgAddress.IsNullOrWhiteSpace())
        {
            return false;
        }
        if (await _networkDaoProposalProvider.IsProposalVoteEndedAsync(chainId, status, expiredTime))
        {
            return false;
        }

        return orgIndex.OrgType switch
        {
            NetworkDaoOrgType.Parliament => await _networkDaoOrgService.IsBp(chainId, address),
            NetworkDaoOrgType.Association => orgMemberList.Contains(address),
            _ => true
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="homePageRequest"></param>
    /// <returns></returns>
    public async Task<HomePageResponse> GetHomePageAsync(HomePageRequest homePageRequest)
    {
        var currentTermMiningRewardTask = GetCurrentTermMiningRewardWithCacheAsync(homePageRequest.ChainId);
        var candidateListTask = GetCandidateDetailListWithCacheAsync(homePageRequest.ChainId);
        var proposalTask = new List<Task<Dictionary<string, ExplorerProposalResult>>>
        {
            GetProposalWithCacheAsync(homePageRequest.ChainId, ProposalType.Parliament),
            GetProposalWithCacheAsync(homePageRequest.ChainId, ProposalType.Association),
            GetProposalWithCacheAsync(homePageRequest.ChainId, ProposalType.Referendum),
        };

        // wait async result and get
        var proposals = (await Task.WhenAll(proposalTask))
            .SelectMany(dict => dict.Values)
            .OrderByDescending(p => p.ExpiredTime)
            .ToList();
        var proposal = proposals.MaxBy(k => k.CreateAt);
        var currentTermMiningReward = await currentTermMiningRewardTask;
        var voteCount = proposals
            .Select(p => p.Approvals + p.Rejections + p.Abstentions).Sum();
        var candidateList = (await candidateListTask).Values;
        return new HomePageResponse
        {
            ChainId = homePageRequest.ChainId,
            TreasuryAmount = currentTermMiningReward,
            TotalVoteNums = voteCount.ToString(),
            VotesOnBP = candidateList.Sum(detail => detail.ObtainedVotesAmount).ToString(),
            Proposal = proposal == null
                ? null
                : new HomePageResponse.ProposalInfo
                {
                    DeployTime = proposal.CreateAt.ToUtcMilliSeconds().ToString(),
                    Title = proposal.ContractMethod,
                    Description = "ContractAddress:" +
                                  AddressHelper.ToFullAddress(homePageRequest.ChainId, proposal.ContractAddress),
                    VoteTickets = proposal.Approvals.ToString(),
                    ProposalStatus = proposal.Status
                }
        };
    }


    private async Task<Dictionary<string, CandidateDetail>> GetCandidateDetailListWithCacheAsync(string chainId)
    {
        var cachedData = await _candidateDetailCache.GetOrAddAsync(
            string.Join(CommonConstant.Underline, "CandidateDetailList", chainId),
            () => GetAllCandidatesAsync(chainId),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTime.UtcNow.AddSeconds(_networkDaoOptions.CurrentValue.CurrentTermMiningRewardCacheSeconds)
            });

        return cachedData.ToDictionary(
            kv => kv.Key,
            kv => CandidateDetail.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(kv.Value)));
    }

    private async Task<string> GetCurrentTermMiningRewardWithCacheAsync(string chainId)
    {
        return await _currentTermMiningRewardCache.GetOrAddAsync(
            "CurrentTermMiningReward",
            async () =>
            {
                var (_, tx) = await _contractProvider.CreateCallTransactionAsync(chainId,
                    SystemContractName.ConsensusContract,
                    "GetCurrentTermMiningReward", new Empty());
                var res = await _contractProvider.CallTransactionAsync<string>(chainId, tx);
                return res;
            },
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTime.UtcNow.AddSeconds(_networkDaoOptions.CurrentValue.CurrentTermMiningRewardCacheSeconds)
            });
    }

    private async Task<Dictionary<string, ExplorerProposalResult>> GetProposalWithCacheAsync(string chainId,
        ProposalType proposalType)
    {
        // short-time cache
        var proposalCacheKey = string.Join(CommonConstant.Underline, "ProposalList", chainId, proposalType.ToString());
        var cacheTime = () => new DistributedCacheEntryOptions
        {
            AbsoluteExpiration =
                DateTime.UtcNow.AddSeconds(_networkDaoOptions.CurrentValue.ProposalVoteCountCacheSeconds)
        };
        // long-time cache
        var proposalBottomCacheKey =
            string.Join(CommonConstant.Underline, "ProposalList_btm", chainId, proposalType.ToString());
        var cacheTimeBottom = () => new DistributedCacheEntryOptions
        {
            // Bottom cache is a long-time cache, use seconds as hours
            AbsoluteExpiration =
                DateTime.UtcNow.AddHours(_networkDaoOptions.CurrentValue.ProposalVoteCountCacheSeconds)
        };

        var refreshAsync = async () =>
        {
            Log.Debug("Refresh start: chainId={ChainId}, type={Type}", chainId, proposalType.ToString());
            var proposals = await GetProposalListAsync(chainId, proposalType);
            await _proposalResultCache.SetAsync(proposalCacheKey, proposals, cacheTime());
            await _proposalResultCacheBottom.SetAsync(proposalBottomCacheKey, proposals, cacheTimeBottom());
            Log.Debug("Refresh finish: chainId={ChainId}, type={Type}", chainId, proposalType.ToString());
            return proposals;
        };

        return await _proposalResultCache.GetOrAddAsync(proposalCacheKey,
            () =>
            {
                Log.Debug("GetOrAdd start: chainId={ChainId}, type={Type}", chainId, proposalType.ToString());
                var refreshTask = refreshAsync(); // to refresh async
                var existsData = _proposalResultCacheBottom.Get(proposalBottomCacheKey);
                Log.Debug("GetOrAdd end: chainId={ChainId}, type={Type}", chainId, proposalType.ToString());
                return Task.FromResult(existsData); // return values from long-time cache
            }, () => cacheTime());
    }

    private async Task<Dictionary<string, ExplorerProposalResult>> GetProposalListAsync(string chainId,
        ProposalType proposalType)
    {
        var pageNum = 1;
        var pageSize = 100;
        var proposalResult = new Dictionary<string, ExplorerProposalResult>();
        while (true)
        {
            var pager = await _explorerProvider.GetProposalPagerAsync(chainId,
                new ExplorerProposalListRequest(pageNum, pageSize)
                {
                    ProposalType = proposalType.ToString()
                });

            if (pager.List.IsNullOrEmpty()) break;
            pageNum++;

            foreach (var proposal in pager.List)
            {
                proposalResult.TryAdd(proposal.ProposalId, proposal);
            }

            if (pager.List.Count < pageSize) break;
        }

        return proposalResult;
    }

    /// pubKey => CandidateDetail.Hex
    private async Task<Dictionary<string, string>> GetAllCandidatesAsync(string chainId)
    {
        var (_, tx) = await _contractProvider.CreateCallTransactionAsync(chainId, SystemContractName.ElectionContract,
            "GetPageableCandidateInformation", new PageInformation { Start = 0, Length = 100 });
        var res = await _contractProvider.CallTransactionAsync<GetPageableCandidateInformationOutput>(chainId, tx);
        return res.Value.ToDictionary(detail => detail.CandidateInformation.Pubkey,
            detail => detail.ToByteArray().ToHex());
    }

    private async Task<Dictionary<string, NetworkDaoProposalDto>> GetNetworkDaoProposalsDictionaryAsync(string chainId,
        List<string> proposalIds)
    {
        var proposalList = await GetNetworkDaoProposalsAsync(chainId, proposalIds);

        return proposalList.IsNullOrEmpty()
            ? new Dictionary<string, NetworkDaoProposalDto>()
            : proposalList.ToDictionary(proposalDto => proposalDto.ProposalId);
    }

    private async Task<IReadOnlyList<NetworkDaoProposalDto>> GetNetworkDaoProposalsAsync(string chainId,
        List<string> proposalIds)
    {
        if (proposalIds.IsNullOrEmpty())
        {
            return new List<NetworkDaoProposalDto>();
        }

        var proposals = await _networkDaoProposalProvider
            .GetNetworkDaoProposalsAsync(new GetNetworkDaoProposalsInput
            {
                ChainId = chainId,
                ProposalIds = proposalIds,
                ProposalType = NetworkDaoProposalType.All,
                SkipCount = 0,
                MaxResultCount = DefaultMaxResultCount,
                StartBlockHeight = 0,
                EndBlockHeight = 0
            });
        return proposals?.Items ?? new List<NetworkDaoProposalDto>();
    }
}