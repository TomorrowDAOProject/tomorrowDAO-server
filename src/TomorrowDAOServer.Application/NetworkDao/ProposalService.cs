using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Election;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using AddressHelper = TomorrowDAOServer.Common.AddressHelper;

namespace TomorrowDAOServer.NetworkDao;

public class ProposalService : IProposalService, ISingletonDependency
{
    private readonly ILogger<ProposalService> _logger;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IOptionsMonitor<NetworkDaoOptions> _networkDaoOptions;
    private readonly IDistributedCache<string> _currentTermMiningRewardCache;
    private readonly IObjectMapper _objectMapper;

    // VoteType => count
    private readonly IDistributedCache<Dictionary<string, int>> _voteCountCache;

    // pubKey => CandidateDetail.Hex
    private readonly IDistributedCache<Dictionary<string, string>> _candidateDetailCache;

    public ProposalService(IExplorerProvider explorerProvider, ILogger<ProposalService> logger,
        IContractProvider contractProvider, IDistributedCache<string> currentTermMiningRewardCache,
        IDistributedCache<Dictionary<string, int>> voteCountCache, IOptionsMonitor<NetworkDaoOptions> networkDaoOptions,
        IDistributedCache<Dictionary<string, string>> candidateDetailCache, IObjectMapper objectMapper)
    {
        _explorerProvider = explorerProvider;
        _logger = logger;
        _contractProvider = contractProvider;
        _currentTermMiningRewardCache = currentTermMiningRewardCache;
        _voteCountCache = voteCountCache;
        _networkDaoOptions = networkDaoOptions;
        _candidateDetailCache = candidateDetailCache;
        _objectMapper = objectMapper;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<PagedResultDto<ProposalListResponse>> GetProposalList(ProposalListRequest request)
    {

        var explorerResp = await _explorerProvider.GetProposalPagerAsync(request.ChainId, new ExplorerProposalListRequest
        {
            Status = request.ProposalStatus,
            ProposalType = request.GovernanceType,
            Search = request.Content,
            Address = request.Address
        });

        var items = _objectMapper.Map<List<ExplorerProposalResult>, List<ProposalListResponse>>(explorerResp.List);
        return new PagedResultDto<ProposalListResponse>
        {
            TotalCount = explorerResp.Total,
            Items = items
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
        var proposalTask = _explorerProvider.GetProposalPagerAsync(homePageRequest.ChainId,
            new ExplorerProposalListRequest
            {
                Address = homePageRequest.Address,
                Search = homePageRequest.ProposalId
            });
        var voteCountTasks = new List<Task<Dictionary<string, int>>>
        {
            GetProposalVoteCountWithCacheAsync(homePageRequest.ChainId, ProposalType.Parliament),
            GetProposalVoteCountWithCacheAsync(homePageRequest.ChainId, ProposalType.Association),
            GetProposalVoteCountWithCacheAsync(homePageRequest.ChainId, ProposalType.Referendum),
        };

        // wait async result and get
        var proposal = (await proposalTask).List.FirstOrDefault();
        var currentTermMiningReward = await currentTermMiningRewardTask;
        var voteCount = (await Task.WhenAll(voteCountTasks)).SelectMany(k => k.Values).Sum();
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
            string.Join(CommonConstant.Underline,"CandidateDetailList", chainId), 
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

    private async Task<Dictionary<string, int>> GetProposalVoteCountWithCacheAsync(string chainId,
        ProposalType proposalType)
    {
        return await _voteCountCache.GetOrAddAsync(
            string.Join(CommonConstant.Underline, "ProposalVoteCount", proposalType.ToString()),
            () => GetProposalVoteCountAsync(chainId, proposalType),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTime.UtcNow.AddSeconds(_networkDaoOptions.CurrentValue.ProposalVoteCountCacheSeconds)
            });
    }

    private async Task<Dictionary<string, int>> GetProposalVoteCountAsync(string chainId, ProposalType proposalType)
    {
        var pageNum = 1;
        var pageSize = 100;
        var countDict = new Dictionary<string, int>();
        while (true)
        {
            var pager = await _explorerProvider.GetProposalPagerAsync(chainId,
                new ExplorerProposalListRequest(pageNum++, pageSize)
                {
                    ProposalType = proposalType.ToString()
                });

            if (pager.List.IsNullOrEmpty() || pager.List.Count < pageSize) break;
            var approveCount = pager.List.Sum(p => p.Approvals);
            var rejectCount = pager.List.Sum(p => p.Rejections);
            var abstainCount = pager.List.Sum(p => p.Abstentions);
            countDict[VoteType.Approve.ToString()] =
                countDict.GetValueOrDefault(VoteType.Approve.ToString()) + approveCount;
            countDict[VoteType.Reject.ToString()] =
                countDict.GetValueOrDefault(VoteType.Reject.ToString()) + rejectCount;
            countDict[VoteType.Abstain.ToString()] =
                countDict.GetValueOrDefault(VoteType.Abstain.ToString()) + abstainCount;
        }

        return countDict;
    }

    /// pubKey => CandidateDetail.Hex
    private async Task<Dictionary<string, string>> GetAllCandidatesAsync(string chainId)
    {
        var (_, tx) = await _contractProvider.CreateCallTransactionAsync(chainId, SystemContractName.ElectionContract,
            "GetPageableCandidateInformation", new PageInformation { Start = 0, Length = 100 });
        var res = await _contractProvider.CallTransactionAsync<GetPageableCandidateInformationOutput>(chainId, tx);
        return res.Value.ToDictionary(detail => detail.CandidateInformation.Pubkey, detail => detail.ToByteArray().ToHex());
    }
}