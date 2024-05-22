using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Vote.Dto;
using TomorrowDAOServer.Vote.Index;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using Newtonsoft.Json;
using Nito.AsyncEx;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Providers;
using ProposalType = TomorrowDAOServer.Enums.ProposalType;

namespace TomorrowDAOServer.Proposal;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ProposalService : TomorrowDAOServerAppService, IProposalService
{
    private const string VoteTopSorting = "Amount DESC";
    private const string DecimalString = "8";
    private const string Symbol = "ELF";
    private readonly IObjectMapper _objectMapper;
    private readonly IOptionsMonitor<ProposalTagOptions> _proposalTagOptionsMonitor;
    private readonly IProposalProvider _proposalProvider;
    private readonly IVoteProvider _voteProvider;
    private readonly IDAOProvider _DAOProvider;
    private readonly IProposalAssistService _proposalAssistService;
    private readonly ILogger<ProposalProvider> _logger;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IScriptService _scriptService;
    private const int ProposalOnceWithdrawMax = 500;
    private Dictionary<string, Tuple<List<string>, long>> _hcDic = new();

    public ProposalService(IObjectMapper objectMapper, IProposalProvider proposalProvider, IVoteProvider voteProvider,
        IExplorerProvider explorerProvider, IGraphQLProvider graphQlProvider, IScriptService scriptService,
        IProposalAssistService proposalAssistService,
        IDAOProvider DAOProvider, IOptionsMonitor<ProposalTagOptions> proposalTagOptionsMonitor,
        ILogger<ProposalProvider> logger)
    {
        _objectMapper = objectMapper;
        _proposalProvider = proposalProvider;
        _voteProvider = voteProvider;
        _proposalTagOptionsMonitor = proposalTagOptionsMonitor;
        _logger = logger;
        _DAOProvider = DAOProvider;
        _proposalAssistService = proposalAssistService;
        _explorerProvider = explorerProvider;
        _graphQlProvider = graphQlProvider;
        _scriptService = scriptService;
    }

    public async Task<ProposalPagedResultDto<ProposalDto>> QueryProposalListAsync(QueryProposalListInput input)
    {
        var councilMemberCountTask = GetHighCouncilMemberCountAsync(input.IsNetworkDao, input.ChainId, input.DaoId);
        var (total, proposalList) = await GetProposalListFromMultiSourceAsync(input);
        if (proposalList.IsNullOrEmpty())
        {
            return new ProposalPagedResultDto<ProposalDto>();
        }

        //query proposal vote infos
        var proposalIds = proposalList.FindAll(item => item.ProposalSource == ProposalSourceEnum.TMRWDAO)
            .Select(item => item.ProposalId).ToList();
        var voteItemsMap = await _voteProvider.GetVoteItemsAsync(input.ChainId, proposalIds);
        var daoIndex = await _DAOProvider.GetAsync(new GetDAOInfoInput
        {
            ChainId = input.ChainId,
            DAOId = input.DaoId
        });
        var tokenInfo = await _explorerProvider.GetTokenInfoAsync(input.ChainId, daoIndex?.GovernanceToken ?? string.Empty);
        var symbol = tokenInfo.Symbol;
        var symbolDecimal = tokenInfo.Decimals.ToString();
        var voteSchemeDic =
            await _voteProvider.GetVoteSchemeDicAsync(new GetVoteSchemeInput { ChainId = input.ChainId });
        await councilMemberCountTask;
        var councilMemberCount = councilMemberCountTask.Result;
        foreach (var proposal in proposalList.Where(proposal => proposal.ProposalSource == ProposalSourceEnum.TMRWDAO))
        {
            if (voteItemsMap.TryGetValue(proposal.ProposalId, out var voteInfo))
            {
                _objectMapper.Map(voteInfo, proposal);
            }

            if (proposal.ProposalType == ProposalType.Advisory.ToString())
            {
                proposal.ExecuteStartTime = null;
                proposal.ExecuteEndTime = null;
                proposal.ExecuteTime = null;
            }

            if (proposal.ProposalStatus != ProposalStatus.Executed.ToString())
            {
                proposal.ExecuteTime = null;
            }

            if (voteSchemeDic.TryGetValue(proposal.VoteSchemeId, out var indexerVoteScheme))
            {
                proposal.VoteMechanismName = indexerVoteScheme.VoteMechanism.ToString();
                await CalculateRealVoteCountAsync(proposal, indexerVoteScheme, symbol, symbolDecimal);
            }

            await CalculateHcRealVoterCountAsync(proposal, councilMemberCount);

            proposal.Symbol = symbol;
            proposal.Decimals = symbolDecimal;
        }

        foreach (var proposal in proposalList.Where(proposal =>
                     proposal.ProposalSource == ProposalSourceEnum.ONCHAIN_PARLIAMENT))
        {
            await CalculateRealBpVoteCountAsync(proposal, councilMemberCount);
            proposal.Symbol = symbol;
            proposal.Decimals = symbolDecimal;
        }

        var proposalPagedResultDto = new ProposalPagedResultDto<ProposalDto>
        {
            Items = proposalList,
            TotalCount = total,
        };
        if (input.IsNetworkDao)
        {
            proposalPagedResultDto.PreviousPageInfo = input.PageInfo;
            proposalPagedResultDto.NextPageInfo = CalcNewPageInfo(proposalList, input.PageInfo);
        }

        return proposalPagedResultDto;
    }

    private static Task CalculateHcRealVoterCountAsync(ProposalDto proposal, int councilMemberCount)
    {
        if (proposal.GovernanceMechanism == GovernanceMechanism.HighCouncil.ToString()
            && proposal.ProposalSource != ProposalSourceEnum.ONCHAIN_REFERENDUM
            && proposal.ProposalSource != ProposalSourceEnum.ONCHAIN_ASSOCIATION)
        {
            proposal.MinimalRequiredThreshold =
                Convert.ToInt64(Math.Ceiling((decimal)proposal.MinimalRequiredThreshold / CommonConstant.AbstractVoteTotal * councilMemberCount));
        }

        return Task.CompletedTask;
    }

    private async Task<int> GetHighCouncilMemberCountAsync(bool isNetworkDao, string chainId, string daoId)
    {
        var count = 0;
        try
        {
            if (isNetworkDao)
            {
                var bpList = await _graphQlProvider.GetBPAsync(chainId);
                count = bpList.IsNullOrEmpty() ? 0 : bpList.Count;
            }
            else
            {
                //TODO HC Count
                return CommonConstant.HCCount;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get High Council member count error, daoId={0}", daoId);
        }

        return count;
    }

    private static Task CalculateRealVoteCountAsync(ProposalDto proposal, IndexerVoteSchemeInfo indexerVoteScheme,
        string symbol, string symbolDecimalStr)
    {
        if (indexerVoteScheme?.VoteMechanism == null || indexerVoteScheme.VoteMechanism != VoteMechanism.TOKEN_BALLOT ||
            symbolDecimalStr.IsNullOrWhiteSpace() || !int.TryParse(symbolDecimalStr, out int symbolDecimal))
        {
            return Task.CompletedTask;
        }

        var pow = (decimal)Math.Pow(10, symbolDecimal);

        proposal.VotesAmount = Convert.ToInt64(Math.Round(proposal.VotesAmount / pow, MidpointRounding.AwayFromZero));
        proposal.ApprovedCount =
            Convert.ToInt64(Math.Round(proposal.ApprovedCount / pow, MidpointRounding.AwayFromZero));
        proposal.RejectionCount =
            Convert.ToInt64(Math.Round(proposal.RejectionCount / pow, MidpointRounding.AwayFromZero));
        proposal.AbstentionCount =
            Convert.ToInt64(Math.Round(proposal.AbstentionCount / pow, MidpointRounding.AwayFromZero));
        proposal.MinimalVoteThreshold =
            Convert.ToInt64(Math.Round(proposal.MinimalVoteThreshold / pow, MidpointRounding.AwayFromZero));
        return Task.CompletedTask;
    }

    private static Task CalculateRealBpVoteCountAsync(ProposalDto proposal, int bpCont)
    {
        if (proposal.ProposalSource != ProposalSourceEnum.ONCHAIN_PARLIAMENT)
        {
            return Task.CompletedTask;
        }

        var pow = 10000;
        proposal.MinimalVoteThreshold =
            Convert.ToInt64(Math.Round((decimal)proposal.MinimalVoteThreshold / pow * bpCont,
                MidpointRounding.AwayFromZero));
        proposal.MinimalApproveThreshold =
            Convert.ToInt64(Math.Round((decimal)proposal.MinimalApproveThreshold / pow * bpCont,
                MidpointRounding.AwayFromZero));
        proposal.MaximalRejectionThreshold = Convert.ToInt64(
            Math.Round((decimal)proposal.MaximalRejectionThreshold / pow * bpCont, MidpointRounding.AwayFromZero));
        proposal.MaximalAbstentionThreshold = Convert.ToInt64(
            Math.Round((decimal)proposal.MaximalAbstentionThreshold / pow * bpCont, MidpointRounding.AwayFromZero));
        proposal.MinimalVoteThreshold = Convert.ToInt64(Math.Round((decimal)proposal.MinimalVoteThreshold / pow,
            MidpointRounding.AwayFromZero));
        proposal.MinimalRequiredThreshold = Convert.ToInt64(Math.Round((decimal)proposal.MinimalRequiredThreshold / pow,
            MidpointRounding.AwayFromZero));
        return Task.CompletedTask;
    }


    private async Task<Tuple<long, List<ProposalDto>>> GetProposalListFromMultiSourceAsync(QueryProposalListInput input)
    {
        if (!input.IsNetworkDao || (input.ProposalType != null && input.ProposalType != ProposalType.OnChain))
        {
            return await GetProposalListAsync(input, ProposalSourceEnum.TMRWDAO);
        }

        var tasks = new List<Task<Tuple<long, List<ProposalDto>>>>();
        tasks.Add(GetProposalListAsync(input, ProposalSourceEnum.TMRWDAO));
        tasks.Add(GetProposalListAsync(input, ProposalSourceEnum.ONCHAIN_PARLIAMENT));
        tasks.Add(GetProposalListAsync(input, ProposalSourceEnum.ONCHAIN_REFERENDUM));
        tasks.Add(GetProposalListAsync(input, ProposalSourceEnum.ONCHAIN_ASSOCIATION));

        await tasks.WhenAll();
        var proposalDtoList = new List<ProposalDto>();
        long total = 0;
        foreach (var task in tasks)
        {
            total += task.Result.Item1;
            if (!task.Result.Item2.IsNullOrEmpty())
            {
                proposalDtoList.AddRange(task.Result.Item2);
            }
        }

        var list = proposalDtoList.OrderByDescending(item => item.DeployTime).Take(input.MaxResultCount).ToList();
        return new Tuple<long, List<ProposalDto>>(total, list);
    }

    private static PageInfo CalcNewPageInfo(List<ProposalDto> proposalList, [CanBeNull] PageInfo pageInfo)
    {
        var newPageInfo = new PageInfo
        {
            ProposalSkipCount = pageInfo?.ProposalSkipCount == null
                ? new Dictionary<ProposalSourceEnum, int>()
                : new Dictionary<ProposalSourceEnum, int>(pageInfo.ProposalSkipCount)
        };
        var skipCount = newPageInfo.ProposalSkipCount;
        foreach (var proposalDto in proposalList)
        {
            var count = skipCount.GetOrDefault(proposalDto.ProposalSource);
            count = count == 0 ? 1 : count;
            skipCount[proposalDto.ProposalSource] = count + 1;
        }

        return newPageInfo;
    }

    private async Task<Tuple<long, List<ProposalDto>>> GetProposalListAsync(QueryProposalListInput input,
        ProposalSourceEnum proposalSource)
    {
        try
        {
            var skipCount = 0;
            if (input != null && input.PageInfo != null && !input.PageInfo.ProposalSkipCount.IsNullOrEmpty() &&
                input.PageInfo.ProposalSkipCount.ContainsKey(proposalSource))
            {
                skipCount = input.PageInfo.ProposalSkipCount[proposalSource];
            }

            if (proposalSource == ProposalSourceEnum.TMRWDAO)
            {
                //if Network DAO, use memory paging parameters; otherwise, use the input.SkipCount.
                if (input.IsNetworkDao)
                {
                    input.SkipCount = skipCount;
                }

                var (total, proposalIndexList) = await _proposalProvider.GetProposalListAsync(input);
                var proposalDtos = _objectMapper.Map<List<ProposalIndex>, List<ProposalDto>>(proposalIndexList);
                return new Tuple<long, List<ProposalDto>>(total, proposalDtos);
            }

            // can not find determinant status to map between network dao and tmr dao
            // so return empty list in
            var proposalType = _explorerProvider.GetProposalType(proposalSource);
            var proposalStatus = _explorerProvider.GetProposalStatus(input.ProposalStatus, null);
            if (string.IsNullOrEmpty(proposalStatus))
            {
                return new Tuple<long, List<ProposalDto>>(0, new List<ProposalDto>());
            }
            var explorerrResponse = await _explorerProvider.GetProposalPagerAsync(CommonConstant.MainChainId,
                new ExplorerProposalListRequest
                {
                    PageSize = input.MaxResultCount,
                    PageNum = skipCount == 0 ? 1 : skipCount,
                    ProposalType = proposalType.ToString(),
                    Status = proposalStatus,
                    IsContract = 0,
                    Address = null,
                    Search = input.Content
                });
            var proposalList = ConvertToProposal(explorerrResponse.List, input, proposalSource);
            return new Tuple<long, List<ProposalDto>>(explorerrResponse.Total, proposalList);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetProposalList from Source:{source} error, input={daoId}", proposalSource,
                JsonConvert.SerializeObject(input));
            return new Tuple<long, List<ProposalDto>>(0, new List<ProposalDto>());
        }
    }

    private static List<ProposalDto> ConvertToProposal(List<ExplorerProposalResult> explorerProposalList,
        QueryProposalListInput input, ProposalSourceEnum proposalSource)
    {
        if (explorerProposalList.IsNullOrEmpty())
        {
            return new List<ProposalDto>();
        }

        var proposalIndexList = new List<ProposalDto>(explorerProposalList.Count);
        proposalIndexList.AddRange(explorerProposalList.Select(explorerProposal =>
        {
            long.TryParse(explorerProposal.OrganizationInfo?.ReleaseThreshold?.MinimalVoteThreshold,
                out long minimalRequiredThreshold);
            long.TryParse(explorerProposal.OrganizationInfo?.ReleaseThreshold?.MinimalVoteThreshold,
                out long minimalVoteThreshold);
            long.TryParse(explorerProposal.OrganizationInfo?.ReleaseThreshold?.MinimalApprovalThreshold,
                out long minimalApproveThreshold);
            long.TryParse(explorerProposal.OrganizationInfo?.ReleaseThreshold?.MinimalApprovalThreshold,
                out long maximalRejectionThreshold);
            long.TryParse(explorerProposal.OrganizationInfo?.ReleaseThreshold?.MinimalApprovalThreshold,
                out long maximalAbstentionThreshold);
            long.TryParse(explorerProposal.Approvals, out long approvals);
            long.TryParse(explorerProposal.Abstentions, out long abstentions);
            return new ProposalDto
            {
                ChainId = input.ChainId,
                DAOId = input.DaoId,
                ProposalId = explorerProposal.ProposalId,
                ProposalTitle = explorerProposal.ProposalId,
                ProposalDescription = null,
                ForumUrl = string.Empty,
                ProposalType = ProposalType.OnChain.ToString(),
                ActiveStartTime = explorerProposal.CreateAt,
                ActiveEndTime = default,
                ExecuteStartTime = default,
                ExecuteEndTime = explorerProposal.ExpiredTime,
                //ProposalStatus = ProposalStatus.Empty,
                //ProposalStage = ProposalStage.Default,
                ProposalStatusForOnChain = explorerProposal.Status,
                Proposer = explorerProposal.Proposer,
                SchemeAddress = null,
                Transaction = new ExecuteTransaction
                {
                    ToAddress = explorerProposal.ContractAddress,
                    ContractMethodName = explorerProposal.ContractMethod,
                },
                VoteSchemeId = null,
                VoteMechanismName = null,
                VetoProposalId = null,
                DeployTime = explorerProposal.CreateAt,
                ExecuteTime = explorerProposal.ReleasedTime,
                GovernanceMechanism = GovernanceMechanism.HighCouncil.ToString(),
                MinimalRequiredThreshold = minimalRequiredThreshold,
                MinimalVoteThreshold = minimalVoteThreshold,
                MinimalApproveThreshold = minimalApproveThreshold,
                MaximalRejectionThreshold = maximalRejectionThreshold,
                MaximalAbstentionThreshold = maximalAbstentionThreshold,
                //VoterCount = 0,
                VotesAmount = approvals + explorerProposal.Rejections + abstentions,
                ApprovedCount = approvals,
                RejectionCount = explorerProposal.Rejections,
                AbstentionCount = abstentions,
                Decimals = DecimalString,
                Symbol = Symbol,
                ProposalSource = proposalSource,
            };
        }));
        return proposalIndexList;
    }

    public async Task<ProposalDetailDto> QueryProposalDetailAsync(QueryProposalDetailInput input)
    {
        _logger.LogInformation("ProposalService QueryProposalDetailAsync daoid:{ProposalId} start", input.ProposalId);
        var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, input.ProposalId);
        if (proposalIndex == null)
        {
            return new ProposalDetailDto();
        }

        _logger.LogInformation(
            "ProposalService QueryProposalDetailAsync daoid:{ProposalId} proposalIndex {proposalIndex}:",
            input.ProposalId, JsonConvert.SerializeObject(proposalIndex));
        var proposalDetailDto = _objectMapper.Map<ProposalIndex, ProposalDetailDto>(proposalIndex);
        var voteSchemeDic =
            await _voteProvider.GetVoteSchemeDicAsync(new GetVoteSchemeInput { ChainId = input.ChainId });
        var daoIndex = await _DAOProvider.GetAsync(new GetDAOInfoInput
        {
            ChainId = input.ChainId,
            DAOId = proposalDetailDto.DAOId
        });
        var councilMemberCountTask =
            GetHighCouncilMemberCountAsync(daoIndex.IsNetworkDAO, input.ChainId, proposalDetailDto.DAOId);
        var tokenInfo = await _explorerProvider.GetTokenInfoAsync(input.ChainId, daoIndex?.GovernanceToken ?? string.Empty);
        var symbol = tokenInfo.Symbol;
        var symbolDecimal = tokenInfo.Decimals.ToString();
        var voteInfos = await _voteProvider.GetVoteItemsAsync(input.ChainId, new List<string> { input.ProposalId });
        await councilMemberCountTask;
        var councilMemberCount = councilMemberCountTask.Result;
        if (voteInfos.TryGetValue(input.ProposalId, out var voteInfo))
        {
            _objectMapper.Map(voteInfo, proposalDetailDto);
        }

        if (voteSchemeDic.TryGetValue(proposalDetailDto.VoteSchemeId, out var indexerVoteScheme))
        {
            proposalDetailDto.VoteMechanismName = indexerVoteScheme.VoteMechanism.ToString();
            await CalculateRealVoteCountAsync(proposalDetailDto, indexerVoteScheme, symbol, symbolDecimal);
        }

        await CalculateHcRealVoterCountAsync(proposalDetailDto, councilMemberCount);
        proposalDetailDto.Symbol = symbol;
        proposalDetailDto.Decimals = symbolDecimal;
        proposalDetailDto.ProposalLifeList = _proposalAssistService.ConvertProposalLifeList(proposalIndex);
        _logger.LogInformation("ProposalService QueryProposalDetailAsync daoid:{ProposalId} voteInfos {voteInfos}:",
            input.ProposalId, JsonConvert.SerializeObject(voteInfos));

        var voteRecords = await _voteProvider.GetLimitVoteRecordAsync(new GetLimitVoteRecordInput
        {
            ChainId = input.ChainId,
            VotingItemId = input.ProposalId,
            Sorting = VoteTopSorting
        });
        _logger.LogInformation("ProposalService QueryProposalDetailAsync daoid:{ProposalId} voteRecords {voteRecords}:",
            input.ProposalId, JsonConvert.SerializeObject(voteRecords));
        proposalDetailDto.VoteTopList = _objectMapper.Map<List<IndexerVoteRecord>, List<VoteRecordDto>>(voteRecords);
        if (proposalDetailDto.ProposalType == ProposalType.Advisory.ToString())
        {
            proposalDetailDto.ExecuteStartTime = null;
            proposalDetailDto.ExecuteEndTime = null;
            proposalDetailDto.ExecuteTime = null;
        }

        if (proposalDetailDto.ProposalStatus != ProposalStatus.Executed.ToString())
        {
            proposalDetailDto.ExecuteTime = null;
        }

        return proposalDetailDto;
    }

    public async Task<MyProposalDto> QueryMyInfoAsync(QueryMyProposalInput input)
    {
        return string.IsNullOrEmpty(input.ProposalId)
            ? await QueryDaoMyInfoAsync(input)
            : await QueryProposalMyInfoAsync(input);
    }

    private async Task<MyProposalDto> QueryProposalMyInfoAsync(QueryMyProposalInput input)
    {
        var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, input.ProposalId);
        if (proposalIndex == null)
        {
            return new MyProposalDto{ChainId = input.ChainId};
        }

        var myProposalDto = _objectMapper.Map<ProposalIndex, MyProposalDto>(proposalIndex);
        var daoIndex = await _DAOProvider.GetAsync(new GetDAOInfoInput { ChainId = input.ChainId, DAOId = proposalIndex.DAOId });
        var tokenInfo = await _explorerProvider.GetTokenInfoAsync(input.ChainId, daoIndex?.GovernanceToken ?? string.Empty);
        var voteRecords = await _voteProvider.GetLimitVoteRecordAsync(new GetLimitVoteRecordInput
        {
            ChainId = input.ChainId, VotingItemId = input.ProposalId, Voter = input.Address, Limit = 1
        });
        
        myProposalDto.Symbol = tokenInfo.Symbol;
        myProposalDto.Decimal = tokenInfo.Decimals.ToString();
        if (voteRecords.IsNullOrEmpty())
        {
            myProposalDto.CanVote = await CanVote(daoIndex, proposalIndex, input.Address);
            return myProposalDto;
        }

        var voteRecord = voteRecords[0];
        myProposalDto.CanVote = false;
        if (VoteMechanism.UNIQUE_VOTE == proposalIndex.VoteMechanism)
        {
            myProposalDto.votesAmountUniqueVote = 1;
            return myProposalDto;
        }
        
        myProposalDto.StakeAmount = voteRecord.Amount;
        myProposalDto.votesAmountTokenBallot = voteRecord.Amount;
        var withdrawVotingItemIdLis = await GetWithdrawVotingItemIdLis(input.ChainId, input.DAOId, input.Address);
        if (!CanWithdraw(proposalIndex.ActiveEndTime, withdrawVotingItemIdLis, input.ProposalId))
        {
            return myProposalDto;
        }

        myProposalDto.AvailableUnStakeAmount = voteRecord.Amount;
        myProposalDto.WithdrawList = new List<WithdrawDto>
        {
            new() { ProposalIdList = new List<string>{input.ProposalId}, WithdrawAmount = voteRecord.Amount }
        };
        return myProposalDto;
    }

    private async Task<MyProposalDto> QueryDaoMyInfoAsync(QueryMyProposalInput input)
    {
        var myProposalDto = new MyProposalDto { ChainId = input.ChainId };
        var daoIndex = await _DAOProvider.GetAsync(new GetDAOInfoInput { ChainId = input.ChainId, DAOId = input.DAOId });
        var tokenInfo = await _explorerProvider.GetTokenInfoAsync(input.ChainId, daoIndex?.GovernanceToken ?? string.Empty);
        myProposalDto.Symbol = tokenInfo.Symbol;
        myProposalDto.Decimal = tokenInfo.Decimals;
        var voteRecords = await _voteProvider.GetAllVoteRecordAsync(
            new GetAllVoteRecordInput { ChainId = input.ChainId, DAOId = input.DAOId, Voter = input.Address });
        if (voteRecords.IsNullOrEmpty())
        {
            return myProposalDto;
        }

        var withdrawList = new List<WithdrawDto>();
        var proposalIdList = new List<string>();
        var withdrawAmount = 0L;
        var withdrawVotingItemIdLis = await GetWithdrawVotingItemIdLis(input.ChainId, input.DAOId, input.Address);
        
        myProposalDto.votesAmountUniqueVote = voteRecords.Count(voteRecord => voteRecord.VoteMechanism == VoteMechanism.UNIQUE_VOTE);
        foreach (var voteRecord in voteRecords.Where(voteRecord => voteRecord.VoteMechanism == VoteMechanism.TOKEN_BALLOT))
        {
            myProposalDto.votesAmountTokenBallot += voteRecord.Amount;
            myProposalDto.StakeAmount += voteRecord.Amount;
            if (!CanWithdraw(voteRecord.EndTime, withdrawVotingItemIdLis, input.ProposalId))
            {
                continue;
            }

            myProposalDto.AvailableUnStakeAmount += voteRecord.Amount;
            if (ProposalOnceWithdrawMax == proposalIdList.Count)
            {
                withdrawList.Add(new WithdrawDto { ProposalIdList = new List<string>(proposalIdList), WithdrawAmount = withdrawAmount });
                proposalIdList = new List<string>();
                withdrawAmount = 0L;
            }
            else
            {
                proposalIdList.Add(voteRecord.VotingItemId);
                withdrawAmount += voteRecord.Amount;
            }
        }

        if (!proposalIdList.IsNullOrEmpty())
        {
            withdrawList.Add(new WithdrawDto { ProposalIdList = new List<string>(proposalIdList), WithdrawAmount = withdrawAmount });
        }
        myProposalDto.WithdrawList = withdrawList;
        return myProposalDto;
    }

    public async Task<VoteHistoryDto> QueryVoteHistoryAsync(QueryVoteHistoryInput input)
    {
        var voteHistoryDto = new VoteHistoryDto { ChainId = input.ChainId };
        var voteRecords = await _voteProvider.GetPageVoteRecordAsync(new GetPageVoteRecordInput
        {
            ChainId = input.ChainId, DaoId = input.DAOId, Voter = input.Address,
            VotingItemId = input.ProposalId, SkipCount = input.SkipCount, MaxResultCount = input.MaxResultCount,
            VoteOption = input.VoteOption
        });
        if (voteRecords.IsNullOrEmpty())
        {
            return voteHistoryDto;
        }
        
        var votingItemIds = voteRecords.Select(x => x.VotingItemId).ToList();
        var voteInfos = await _voteProvider.GetVoteItemsAsync(input.ChainId, votingItemIds);
        var proposalInfos = (await _proposalProvider.GetProposalByIdsAsync(input.ChainId, votingItemIds))
            .ToDictionary(x => x.ProposalId, x => x);
        var historyList = _objectMapper.Map<List<IndexerVoteRecord>, List<IndexerVoteHistoryDto>>(voteRecords);
        foreach (var history in historyList)
        {
            history.Executer = voteInfos.TryGetValue(history.ProposalId, out var voteInfo) ? voteInfo.Executer : string.Empty;
            history.ProposalTitle = proposalInfos.TryGetValue(history.ProposalId, out var proposalIndex) ? proposalIndex.ProposalTitle : string.Empty;
        }
        voteHistoryDto.Items = historyList;
        return voteHistoryDto;
    }

    public async Task<ProposalPagedResultDto<ProposalBasicDto>> QueryExecutableProposalsAsync(QueryExecutableProposalsInput input)
    {
        _logger.LogInformation("query executable proposals,  daoid={0}, proposalor={1}", input.DaoId, input.Proposer);
        var proposalIndex =
            await _proposalProvider.QueryProposalsByProposerAsync(new QueryProposalByProposerRequest
            {
                ChainId = input.ChainId,
                DaoId = input.DaoId,
                //ProposalStatus = ProposalStatus,
                ProposalStage = ProposalStage.Execute,
                Proposer = input.Proposer,
                SkipCount = input.SkipCount,
                MaxResultCount = input.MaxResultCount
            });
        if (proposalIndex == null || proposalIndex.Item2.IsNullOrEmpty())
        {
            return new ProposalPagedResultDto<ProposalBasicDto>();
        }
        _logger.LogInformation("query executable proposals result:{0}", JsonConvert.SerializeObject(proposalIndex));
        
        var proposalDtoList = _objectMapper.Map<List<ProposalIndex>, List<ProposalBasicDto>>(proposalIndex.Item2);

        return new ProposalPagedResultDto<ProposalBasicDto>
        {
            Items = proposalDtoList,
            TotalCount = proposalIndex.Item1,
        };
    }
    
    private async Task<bool> CanVote(DAOIndex daoIndex, ProposalBase proposalIndex, string address, bool voted = false)
    {
        if (voted || ProposalStage.Active != proposalIndex.ProposalStage)
        {
            return false;
        }
        
        if (GovernanceMechanism.Referendum == proposalIndex.GovernanceMechanism)
        {
            return true;
        }

        if (daoIndex.IsNetworkDAO)
        {
            return (await _graphQlProvider.GetBPAsync(daoIndex.ChainId)).Contains(address);
        }

        if (_hcDic.TryGetValue(daoIndex.Id, out var value) && DateTime.UtcNow.ToUtcMilliSeconds() - value.Item2 <= 10 * 60 * 1000)
        {
            return value.Item1.Contains(address);
        }

        var currentHc = await _scriptService.GetCurrentHCAsync(daoIndex.ChainId, daoIndex.Id);
        _hcDic[daoIndex.Id] = new Tuple<List<string>, long>(currentHc, DateTime.UtcNow.ToUtcMilliSeconds());
        return currentHc.Contains(address);
    }

    private static bool CanWithdraw(DateTime endTime, List<string> votingItemIdList, string proposalId, bool isUniqueVote = false)
    {
        return !isUniqueVote && DateTime.Now > endTime && !votingItemIdList.Contains(proposalId);
    }

    private async Task<List<string>> GetWithdrawVotingItemIdLis(string chainId, string DAOId, string address)
    {
        var withdrawVotingItemIdList = new List<string>();
        var withdrawnInfo = await _voteProvider.GetVoteWithdrawnAsync(chainId, DAOId, address);
        foreach (var withdrawnDto in withdrawnInfo)
        {
            withdrawVotingItemIdList.AddRange(withdrawnDto.VotingItemIdList);
        }

        return withdrawVotingItemIdList;
    }
}