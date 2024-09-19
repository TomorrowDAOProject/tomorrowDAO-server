using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Proposal;

public interface IProposalAssistService
{
    public Task<Tuple<List<ProposalIndex>, List<IndexerProposal>>> ConvertProposalList(string chainId, List<IndexerProposal> list);
    // public Task<List<ProposalIndex>> ConvertProposalList(string chainId, List<ProposalIndex> list);
    public Task<List<ProposalIndex>> NewConvertProposalList(string chainId, List<ProposalIndex> list);
    public List<ProposalLifeDto> ConvertProposalLifeList(ProposalIndex proposalIndex);
    public Task ReRunProposalList(string chainId, List<string> proposalIds);
    public Task ChangeRegex(string pattern);
}

public class ProposalAssistService : TomorrowDAOServerAppService, IProposalAssistService
{
    private readonly ILogger<ProposalAssistService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IVoteProvider _voteProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IScriptService _scriptService;
    private Dictionary<string, VoteMechanism> _voteMechanisms = new();
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private Regex _regex;

    public ProposalAssistService(ILogger<ProposalAssistService> logger, IObjectMapper objectMapper, IVoteProvider voteProvider,
        IProposalProvider proposalProvider, IGraphQLProvider graphQlProvider, IScriptService scriptService, 
        IOptionsMonitor<RankingOptions> rankingOptions)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _voteProvider = voteProvider;
        _proposalProvider = proposalProvider;
        _graphQlProvider = graphQlProvider;
        _scriptService = scriptService;
        _rankingOptions = rankingOptions;
        _regex = new Regex(CommonConstant.DescriptionPattern, RegexOptions.Compiled);
    }

    public async Task<Tuple<List<ProposalIndex>, List<IndexerProposal>>> ConvertProposalList(string chainId, List<IndexerProposal> list)
    {
        var rankingDaoIds = _rankingOptions.CurrentValue.DaoIds;
        var rankingProposalList = new List<IndexerProposal>();
        var proposalIds = list.Select(x => x.ProposalId).ToList();
        var serverProposalList = await _proposalProvider.GetProposalByIdsAsync(chainId, proposalIds);
        var serverProposalDic = serverProposalList.ToDictionary(x => x.ProposalId, x => x);
        foreach (var proposal in list)
        {
            if (rankingDaoIds.Contains(proposal.DAOId))
            {
                proposal.ProposalCategory = _regex.IsMatch(proposal.ProposalDescription.Trim()) ? ProposalCategory.Ranking : ProposalCategory.Normal;
            }
            
            if (!serverProposalDic.TryGetValue(proposal.ProposalId, out var serverProposal))
            {
                if (rankingDaoIds.Contains(proposal.DAOId) && ProposalCategory.Ranking == proposal.ProposalCategory)
                {
                    rankingProposalList.Add(proposal);
                    _logger.LogInformation("RankingProposalNeedToGenerate proposalId {proposalId} description {description}", 
                        proposal.ProposalId, proposal.ProposalDescription);
                }
            }
            else
            {
                if (serverProposal.ProposalStage.CompareTo(proposal.ProposalStage) <= 0)
                {
                    continue;
                }
                proposal.ProposalStatus = serverProposal.ProposalStatus;
                proposal.ProposalStage = serverProposal.ProposalStage;
            }
        }

        var proposalList = _objectMapper.Map<List<IndexerProposal>, List<ProposalIndex>>(list);
        return new Tuple<List<ProposalIndex>, List<IndexerProposal>>(proposalList, rankingProposalList);
    }

    public async Task<List<ProposalIndex>> NewConvertProposalList(string chainId, List<ProposalIndex> list)
    {
        var tasks = list!.Select(async proposal =>
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
        return list;
    }

    // public async Task<List<ProposalIndex>> ConvertProposalList(string chainId, List<ProposalIndex> list)
    // {
    //     var bpCount = (await _graphQlProvider.GetBPAsync(chainId)).Count;
    //     var proposalIds = list.Select(x => x.ProposalId).ToList();
    //     var voteInfos = await _voteProvider.GetVoteItemsAsync(chainId, proposalIds);
    //     if (_voteMechanisms.IsNullOrEmpty())
    //     {
    //         _voteMechanisms = (await _voteProvider.GetVoteSchemeAsync(new GetVoteSchemeInput { ChainId = chainId }))
    //             .ToDictionary(x => x.VoteSchemeId, x => x.VoteMechanism);
    //     }
    //     
    //     foreach (var proposal in list)
    //     {
    //         var proposalId = proposal.ProposalId;
    //         var vetoProposalId = proposal.VetoProposalId;
    //         var proposalType = proposal.ProposalType;
    //         var proposalStage = proposal.ProposalStage;
    //         var executeStartTime = proposal.ExecuteStartTime;
    //         var executeTime = proposal.ExecuteTime;
    //
    //         voteInfos.TryGetValue(proposalId, out var voteInfo);
    //         if (_voteMechanisms.TryGetValue(proposal.VoteSchemeId, out var voteMechanism))
    //         {
    //             proposal.VoteMechanism = voteMechanism;
    //         }
    //         
    //         switch (proposalType)
    //         {
    //             case ProposalType.Governance:
    //                 switch (proposalStage)
    //                 {
    //                     case ProposalStage.Active:
    //                         _objectMapper.Map(ProcessActiveProposalStage(proposal, voteInfo, bpCount), proposal);
    //                         break;
    //                     case ProposalStage.Pending:
    //                         if (!vetoProposalId.IsNullOrEmpty())
    //                         {
    //                             proposal.ProposalStatus = ProposalStatus.Challenged;
    //                             var vetoProposal = await _proposalProvider.GetProposalByIdAsync(chainId, vetoProposalId);
    //                             var vetoProposalStatus = vetoProposal.ProposalStatus;
    //                             switch (vetoProposal.ProposalStage)
    //                             {
    //                                 case ProposalStage.Active:
    //                                 case ProposalStage.Execute:
    //                                     break;
    //                                 case ProposalStage.Finished:
    //                                     proposal.ProposalStatus = vetoProposalStatus == ProposalStatus.Executed ? ProposalStatus.Vetoed : ProposalStatus.Approved;
    //                                     proposal.ProposalStage = vetoProposalStatus == ProposalStatus.Executed ? ProposalStage.Finished : ProposalStage.Execute;
    //                                     break;
    //                             }
    //                         }
    //                         else if (TimeEnd(executeStartTime))
    //                         {
    //                             proposal.ProposalStatus = ProposalStatus.Approved;
    //                             proposal.ProposalStage = ProposalStage.Execute;
    //                         }
    //                         break;
    //                     case ProposalStage.Execute:
    //                         _objectMapper.Map(ProcessExecuteProposalStage(proposal), proposal);
    //                         break;
    //                 }
    //                 break;
    //             case ProposalType.Veto:
    //                 switch (proposalStage)
    //                 {
    //                     case ProposalStage.Active:
    //                         _objectMapper.Map(ProcessActiveProposalStage(proposal, voteInfo, bpCount), proposal);
    //                         break;
    //                     case ProposalStage.Execute:
    //                         _objectMapper.Map(ProcessExecuteProposalStage(proposal), proposal);
    //                         break;
    //                 }
    //                 break;
    //             case ProposalType.Advisory:
    //                 switch (proposalStage)
    //                 {
    //                     case ProposalStage.Active:
    //                         _objectMapper.Map(ProcessActiveProposalStage(proposal, voteInfo, bpCount), proposal);
    //                         break;
    //                 }
    //                 break;
    //         }
    //     }
    //     
    //     return list;
    // }

    public List<ProposalLifeDto> ConvertProposalLifeList(ProposalIndex proposalIndex)
    {
        var result = new List<ProposalLifeDto>();
        var deployTime = proposalIndex.DeployTime;
        var activeStartTime = proposalIndex.ActiveStartTime;
        if (deployTime < activeStartTime)
        {
            AddProposalLife(ref result, ProposalStage.WaitingActive, ProposalStatus.Published);
        }

        if (DateTime.UtcNow > activeStartTime)
        {
            AddProposalLife(ref result, ProposalStage.Active, ProposalStatus.PendingVote);
        }

        var proposalStatus = proposalIndex.ProposalStatus;
        var isVetoed = proposalIndex.VetoProposalId.IsNullOrEmpty();
        switch (proposalIndex.ProposalStage)
            {
                case ProposalStage.Active:
                    break;
                case ProposalStage.Pending:
                    AddProposalLife(ref result, ProposalStage.Pending, proposalStatus);
                    break;
                case ProposalStage.Execute:
                    if (GovernanceMechanism.HighCouncil == proposalIndex.GovernanceMechanism)
                    {
                        AddProposalLife(ref result, ProposalStage.Pending, isVetoed ? ProposalStatus.Challenged : ProposalStatus.Approved);
                    }
                    AddProposalLife(ref result, ProposalStage.Execute, ProposalStatus.Approved);
                    break;
                case ProposalStage.Finished:
                    switch (proposalStatus)
                    {
                        case ProposalStatus.Rejected:
                        case ProposalStatus.Abstained:
                        case ProposalStatus.BelowThreshold:    
                            break;
                        case ProposalStatus.Vetoed:
                            AddProposalLife(ref result, ProposalStage.Pending, ProposalStatus.Challenged);
                            break;
                        case ProposalStatus.Executed:
                        case ProposalStatus.Expired:
                            if (GovernanceMechanism.HighCouncil == proposalIndex.GovernanceMechanism)
                            {
                                AddProposalLife(ref result, ProposalStage.Pending, isVetoed ? ProposalStatus.Challenged : ProposalStatus.Approved);
                            }
                            AddProposalLife(ref result, ProposalStage.Execute, ProposalStatus.Approved);
                            break;
                    }
                    
                    AddProposalLife(ref result, ProposalStage.Finished, proposalStatus);
                    break;
            }

        return result;
    }

    public async Task ReRunProposalList(string chainId, List<string> proposalIds)
    {
        var list = await _proposalProvider.GetProposalByIdsAsync(chainId, proposalIds);
        var resultList = await NewConvertProposalList(chainId, list);
        if (!resultList.IsNullOrEmpty())
        {
            await _proposalProvider.BulkAddOrUpdateAsync(resultList);
        }
    }

    public Task ChangeRegex(string pattern)
    {
        _regex = new Regex(pattern, RegexOptions.Compiled);
        return Task.CompletedTask;
    }

    private static void AddProposalLife(ref List<ProposalLifeDto> result, ProposalStage proposalStage, ProposalStatus proposalStatus)
    {
        result.AddLast(new ProposalLifeDto
        {
            ProposalStage = MapHelper.MapProposalStageString(proposalStage),
            ProposalStatus = MapHelper.MapProposalStatus(proposalStatus).ToString()
        });
    }

    // private static ProposalIndex ProcessActiveProposalStage(ProposalIndex proposal, IndexerVote voteInfo, long bpCount)
    // {
    //     var governanceMechanism = proposal.GovernanceMechanism;
    //     var proposalType = proposal.ProposalType;
    //     var voteMechanism = proposal.VoteMechanism;
    //     var activeEndTime = proposal.ActiveEndTime;
    //     var totalVote = voteInfo?.VotesAmount ?? 0;
    //     var totalVoter = voteInfo?.VoterCount ?? 0;
    //     var rejectVote = voteInfo?.RejectionCount ?? 0;
    //     var abstainVote = voteInfo?.AbstentionCount ?? 0;
    //     var approveVote = voteInfo?.ApprovedCount ?? 0;
    //
    //     var enoughVoter = totalVoter >= GetRealVoterThreshold(proposal, bpCount);
    //     var enoughVote = rejectVote + abstainVote + approveVote >= proposal.MinimalVoteThreshold;
    //     var isReject = rejectVote * CommonConstant.AbstractVoteTotal > proposal.MaximalRejectionThreshold * totalVote;
    //     var isAbstained = abstainVote * CommonConstant.AbstractVoteTotal > proposal.MaximalAbstentionThreshold *  totalVote;
    //     var isApproved = approveVote * CommonConstant.AbstractVoteTotal > proposal.MinimalApproveThreshold *  totalVote;
    //
    //     if (!TimeEnd(activeEndTime))
    //     {
    //         return proposal;
    //     }
    //
    //     if (!enoughVoter || (VoteMechanism.TOKEN_BALLOT == voteMechanism && !enoughVote))
    //     {
    //         proposal.ProposalStatus = ProposalStatus.BelowThreshold;
    //         proposal.ProposalStage = ProposalStage.Finished;
    //         return proposal;
    //     }
    //     
    //     if (!isApproved)
    //     {
    //         proposal.ProposalStage = ProposalStage.Finished;
    //         proposal.ProposalStatus = isReject ? ProposalStatus.Rejected : isAbstained ? ProposalStatus.Abstained : ProposalStatus.BelowThreshold;
    //         return proposal; 
    //     }
    //
    //     switch (proposalType)
    //     {
    //         case ProposalType.Advisory:
    //             proposal.ProposalStage = ProposalStage.Finished;
    //             proposal.ProposalStatus = ProposalStatus.Approved;
    //             break;
    //         case ProposalType.Governance:
    //             proposal.ProposalStage = governanceMechanism == GovernanceMechanism.Referendum ? ProposalStage.Execute : ProposalStage.Pending;
    //             proposal.ProposalStatus = ProposalStatus.Approved;
    //             break;
    //         case ProposalType.Veto:
    //             proposal.ProposalStage = ProposalStage.Execute;
    //             proposal.ProposalStatus = ProposalStatus.Approved;
    //             break;
    //     }
    //
    //     return proposal;
    // }

    // private static ProposalIndex ProcessExecuteProposalStage(ProposalIndex proposal)
    // {
    //     var executeEndTime = proposal.ExecuteEndTime;
    //     var executeTime = proposal.ExecuteTime;
    //     if (!TimeEnd(executeEndTime))
    //     {
    //         return proposal;
    //     }
    //
    //     proposal.ProposalStatus = IsDateNull(executeTime) ? ProposalStatus.Expired : ProposalStatus.Executed;
    //     proposal.ProposalStage = ProposalStage.Finished;
    //     return proposal;
    // }

    // private static bool TimeEnd(DateTime time) => DateTime.UtcNow > time;
    //
    // private static long GetRealVoterThreshold(ProposalBase proposalIndex, long bpCount)
    // {
    //     if (GovernanceMechanism.Referendum == proposalIndex.GovernanceMechanism)
    //     {
    //         return proposalIndex.MinimalRequiredThreshold;
    //     }
    //
    //     var minCount =  (proposalIndex.IsNetworkDAO ? bpCount : CommonConstant.HCCount) * proposalIndex.MinimalRequiredThreshold; 
    //     return minCount / CommonConstant.AbstractVoteTotal + (minCount % CommonConstant.AbstractVoteTotal == 0 ? 0 : 1);
    // }
    //
    // private static bool IsDateNull(DateTime? time)
    // {
    //     return time == null || time.Value == DateTime.MinValue;
    // }
    
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