using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.User.Provider;
using TomorrowDAOServer.Vote;
using Volo.Abp.Users;
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
    private readonly IUserProvider _userProvider;
    private readonly IElectionProvider _electionProvider;
    private const int ProposalOnceWithdrawMax = 500;
    private Dictionary<string, VoteMechanism> _voteMechanisms = new();

    public ProposalService(IObjectMapper objectMapper, IProposalProvider proposalProvider, IVoteProvider voteProvider,
        IExplorerProvider explorerProvider, IGraphQLProvider graphQlProvider, IScriptService scriptService,
        IProposalAssistService proposalAssistService,
        IDAOProvider DAOProvider, IOptionsMonitor<ProposalTagOptions> proposalTagOptionsMonitor,
        ILogger<ProposalProvider> logger, IUserProvider userProvider, IElectionProvider electionProvider)
    {
        _objectMapper = objectMapper;
        _proposalProvider = proposalProvider;
        _voteProvider = voteProvider;
        _proposalTagOptionsMonitor = proposalTagOptionsMonitor;
        _logger = logger;
        _userProvider = userProvider;
        _electionProvider = electionProvider;
        _DAOProvider = DAOProvider;
        _proposalAssistService = proposalAssistService;
        _explorerProvider = explorerProvider;
        _graphQlProvider = graphQlProvider;
        _scriptService = scriptService;
    }

    public async Task<ProposalPagedResultDto<ProposalDto>> QueryProposalListAsync(QueryProposalListInput input)
    {
        input.ProposalStatus = MapHelper.MapProposalStatus(input.ProposalStatus);
        var (total, proposalList) = await GetProposalListAsync(input);
        if (proposalList.IsNullOrEmpty())
        {
            return new ProposalPagedResultDto<ProposalDto>();
        }

        var governanceMechanism = proposalList[0].GovernanceMechanism;
        var councilMemberCountTask =
            GetHighCouncilMemberCountAsync(input.IsNetworkDao, input.ChainId, input.DaoId, governanceMechanism);
        //query proposal vote infos
        var proposalIds = proposalList.Select(item => item.ProposalId).ToList();
        var voteItemsMap = await _voteProvider.GetVoteItemsAsync(input.ChainId, proposalIds);
        var daoIndex = await _DAOProvider.GetAsync(new GetDAOInfoInput
        {
            ChainId = input.ChainId,
            DAOId = input.DaoId
        });
        var tokenInfo =
            await _explorerProvider.GetTokenInfoAsync(input.ChainId, daoIndex?.GovernanceToken ?? string.Empty);
        var symbol = tokenInfo.Symbol;
        var symbolDecimal = tokenInfo.Decimals;
        var voteSchemeDic =
            await _voteProvider.GetVoteSchemeDicAsync(new GetVoteSchemeInput { ChainId = input.ChainId });
        await councilMemberCountTask;
        var councilMemberCount = councilMemberCountTask.Result;
        foreach (var proposal in proposalList)
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
        var proposalPagedResultDto = new ProposalPagedResultDto<ProposalDto>
        {
            Items = proposalList,
            TotalCount = total,
        };
        
        return proposalPagedResultDto;
    }

    private static Task CalculateHcRealVoterCountAsync(ProposalDto proposal, int councilMemberCount)
    {
        if (proposal.GovernanceMechanism == GovernanceMechanism.HighCouncil.ToString() ||
             proposal.GovernanceMechanism == GovernanceMechanism.Organization.ToString())
        {
            proposal.MinimalRequiredThreshold =
                Convert.ToInt64(Math.Ceiling((decimal)proposal.MinimalRequiredThreshold /
                    CommonConstant.AbstractVoteTotal * councilMemberCount));
        }

        return Task.CompletedTask;
    }

    private async Task<int> GetHighCouncilMemberCountAsync(bool isNetworkDao, string chainId, string daoId,
        string governanceMechanism)
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
                if (GovernanceMechanism.Organization.ToString() == governanceMechanism)
                {
                    var result = await _DAOProvider.GetMemberListAsync(new GetMemberListInput
                    {
                        ChainId = chainId, DAOId = daoId, SkipCount = 0, MaxResultCount = 1
                    });
                    count = (int)result.TotalCount;
                }
                else
                {
                    var hcList = await _electionProvider.GetHighCouncilMembersAsync(chainId, daoId);
                    count = hcList.IsNullOrEmpty() ? 0 : hcList.Count;
                }
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

    private async Task<Tuple<long, List<ProposalDto>>> GetProposalListAsync(QueryProposalListInput input)
    {
        try
        {
            var (total, proposalIndexList) = await _proposalProvider.GetProposalListAsync(input);
            var proposalDtos = _objectMapper.Map<List<ProposalIndex>, List<ProposalDto>>(proposalIndexList);
            return new Tuple<long, List<ProposalDto>>(total, proposalDtos);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetProposalListAsync error, input={daoId}", JsonConvert.SerializeObject(input));
            return new Tuple<long, List<ProposalDto>>(0, new List<ProposalDto>());
        }
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
        proposalDetailDto.Alias = daoIndex.Alias;
        var councilMemberCountTask =
            GetHighCouncilMemberCountAsync(daoIndex.IsNetworkDAO, input.ChainId, proposalDetailDto.DAOId,
                proposalIndex.GovernanceMechanism.ToString());
        var tokenInfo =
            await _explorerProvider.GetTokenInfoAsync(input.ChainId, daoIndex?.GovernanceToken ?? string.Empty);
        var symbol = tokenInfo.Symbol;
        var symbolDecimal = tokenInfo.Decimals;
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

        proposalDetailDto.CanExecute = CanExecute(proposalDetailDto, input.Address);
        return proposalDetailDto;
    }

    private bool CanExecute(ProposalDetailDto proposalDetailDto, string address)
    {
        return proposalDetailDto.Proposer == address
               && proposalDetailDto.ProposalStatus == ProposalStatus.Approved.ToString()
               && proposalDetailDto.ProposalStage == ProposalStage.Execute.ToString()
               && proposalDetailDto.ExecuteStartTime != null &&  proposalDetailDto.ExecuteStartTime <= DateTime.Now
               && proposalDetailDto.ExecuteEndTime != null && proposalDetailDto.ExecuteEndTime >= DateTime.Now;
    }

    public async Task<MyProposalDto> QueryMyInfoAsync(QueryMyProposalInput input)
    {
        input.Address = await GetAndValidateUserAddress(input.ChainId);
        return string.IsNullOrEmpty(input.ProposalId)
            ? await QueryDaoMyInfoAsync(input)
            : await QueryProposalMyInfoAsync(input);
    }

    public async Task<MyProposalDto> QueryProposalMyInfoAsync(QueryMyProposalInput input)
    {
        var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, input.ProposalId);
        var daoIndex = await _DAOProvider.GetAsync(new GetDAOInfoInput { ChainId = input.ChainId, DAOId = input.DAOId });
        if (proposalIndex == null || daoIndex == null)
        {
            return new MyProposalDto { ChainId = input.ChainId };
        }

        var voteRecords = await _voteProvider.GetByVoterAndVotingItemIdsAsync(input.ChainId, input.Address, new List<string>{input.ProposalId});
        var voted = !voteRecords.IsNullOrEmpty();
        var canVote = await CanVote(daoIndex, proposalIndex, input.Address, voted);
        if (daoIndex.GovernanceToken.IsNullOrEmpty())
        {
            return new MyProposalDto { ChainId = input.ChainId,  CanVote = canVote, VotesAmountUniqueVote = canVote ? 0 : 1 };
        }

        var myProposalDto = new MyProposalDto { ChainId = input.ChainId, CanVote = canVote }; 
        var tokenInfo = await _explorerProvider.GetTokenInfoAsync(input.ChainId, daoIndex.GovernanceToken ?? string.Empty);
        myProposalDto.Symbol = tokenInfo.Symbol;
        myProposalDto.Decimal = tokenInfo.Decimals;
        if (!voted)
        {
            return myProposalDto;
        }

        var voteRecord = voteRecords[0];
        myProposalDto.AvailableUnStakeAmount = DateTime.Now > voteRecord.EndTime && !voteRecord.IsWithdraw ? voteRecord.Amount : 0;
        myProposalDto.StakeAmount = voteRecord.IsWithdraw ? 0 : voteRecord.Amount;
        myProposalDto.VotesAmountTokenBallot = voteRecord.Amount;
        if (!voteRecord.IsWithdraw)
        {
            myProposalDto.WithdrawList = new List<WithdrawDto>
            {
                new() { ProposalIdList = new List<string> { input.ProposalId }, WithdrawAmount = voteRecord.Amount }
            };
        }
        return myProposalDto;
    }

    public async Task<MyProposalDto> QueryDaoMyInfoAsync(QueryMyProposalInput input)
    {
        var daoIndex = await _DAOProvider.GetAsync(new GetDAOInfoInput { ChainId = input.ChainId, DAOId = input.DAOId });
        if (daoIndex == null)
        {
            return new MyProposalDto { ChainId = input.ChainId };
        }

        var daoVoterRecord = await _voteProvider.GetDaoVoterRecordAsync(input.ChainId, input.DAOId, input.Address);
        if (daoIndex.GovernanceToken.IsNullOrEmpty())
        {
            return new MyProposalDto { ChainId = input.ChainId, VotesAmountUniqueVote = daoVoterRecord.Count };
        }

        var tokenInfo = await _explorerProvider.GetTokenInfoAsync(input.ChainId, daoIndex.GovernanceToken ?? string.Empty);
        var nonWithdrawVoteRecords = await _voteProvider.GetNonWithdrawVoteRecordAsync(input.ChainId, input.DAOId, input.Address);
        var canWithdrawVoteRecords = nonWithdrawVoteRecords.Where(x => DateTime.Now > x.EndTime).ToList();
        var withdrawList = new List<WithdrawDto>();
        for (var i = 0; i < canWithdrawVoteRecords.Count; i += ProposalOnceWithdrawMax)  
        {  
            var group = canWithdrawVoteRecords.GetRange(i, Math.Min(ProposalOnceWithdrawMax, canWithdrawVoteRecords.Count - i));
            withdrawList.Add(new WithdrawDto
            {
                ProposalIdList = group.Select(x => x.VotingItemId).ToList(),
                WithdrawAmount = group.Sum(x => x.Amount)
            });  
        }

        return new MyProposalDto
        {
            ChainId = input.ChainId, Symbol = tokenInfo.Symbol, Decimal = tokenInfo.Decimals,
            StakeAmount = nonWithdrawVoteRecords.Sum(x => x.Amount),
            AvailableUnStakeAmount = canWithdrawVoteRecords.Sum(x => x.Amount),
            VotesAmountTokenBallot = daoVoterRecord.Amount,
            WithdrawList = withdrawList,
        };
    }

    public async Task<VoteHistoryDto> QueryVoteHistoryAsync(QueryVoteHistoryInput input)
    {
        input.Address = await GetAndValidateUserAddress(input.ChainId);

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
            history.Executer = voteInfos.TryGetValue(history.ProposalId, out var voteInfo)
                ? voteInfo.Executer
                : string.Empty;
            history.ProposalTitle = proposalInfos.TryGetValue(history.ProposalId, out var proposalIndex)
                ? proposalIndex.ProposalTitle
                : string.Empty;
        }

        voteHistoryDto.Items = historyList;
        return voteHistoryDto;
    }

    public async Task<ProposalPagedResultDto<ProposalBasicDto>> QueryExecutableProposalsAsync(
        QueryExecutableProposalsInput input)
    {
        _logger.LogInformation("query executable proposals,  daoId={0}, proposer={1}", input.DaoId, input.Proposer);
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

        switch (proposalIndex.GovernanceMechanism)
        {
            case GovernanceMechanism.Referendum:
                return true;
            case GovernanceMechanism.Organization:
            {
                var member = await _DAOProvider.GetMemberAsync(new GetMemberInput
                    { DAOId = daoIndex.Id, ChainId = daoIndex.ChainId, Address = address });
                return !string.IsNullOrEmpty(member.Address);
            }
        }

        if (daoIndex.IsNetworkDAO)
        {
            return (await _graphQlProvider.GetBPAsync(daoIndex.ChainId)).Contains(address);
        }

        var highCouncilMembers = await _electionProvider.GetHighCouncilMembersAsync(daoIndex.ChainId, daoIndex.Id);
        return highCouncilMembers.Contains(address);
    }

    // private static bool CanWithdraw(DateTime endTime, List<string> votingItemIdList, string proposalId,
    //     bool isUniqueVote = false)
    // {
    //     return !isUniqueVote && DateTime.Now > endTime && !votingItemIdList.Contains(proposalId);
    // }
    //
    // private async Task<List<string>> GetWithdrawVotingItemIdLis(string chainId, string DAOId, string address)
    // {
    //     var withdrawVotingItemIdList = new List<string>();
    //     var withdrawnInfo = await _voteProvider.GetVoteWithdrawnAsync(chainId, DAOId, address);
    //     foreach (var withdrawnDto in withdrawnInfo)
    //     {
    //         withdrawVotingItemIdList.AddRange(withdrawnDto.VotingItemIdList);
    //     }
    //
    //     return withdrawVotingItemIdList;
    // }

    private async Task<string> GetAndValidateUserAddress(string chainId)
    {
        var userId = CurrentUser.GetId();
        var userAddress = await _userProvider.GetUserAddress(userId, chainId);
        if (!userAddress.IsNullOrWhiteSpace())
        {
            return userAddress;
        }

        _logger.LogError("query user address fail, userId={0}, chainId={1}", userId, chainId);
        throw new UserFriendlyException("No user address found");
    }
}