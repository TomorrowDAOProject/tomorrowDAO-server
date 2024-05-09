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
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using Newtonsoft.Json;

namespace TomorrowDAOServer.Proposal;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ProposalService : TomorrowDAOServerAppService, IProposalService
{
    private const string VoteTopSorting = "Amount DESC";
    private readonly IObjectMapper _objectMapper;
    private readonly IOptionsMonitor<ProposalTagOptions> _proposalTagOptionsMonitor;
    private readonly IProposalProvider _proposalProvider;
    private readonly IVoteProvider _voteProvider;
    private readonly IDAOProvider _DAOProvider;
    private readonly IProposalAssistService _proposalAssistService;
    private readonly ILogger<ProposalProvider> _logger;

    public ProposalService(IObjectMapper objectMapper, IProposalProvider proposalProvider, IVoteProvider voteProvider, 
        IProposalAssistService proposalAssistService,
        IDAOProvider DAOProvider, IOptionsMonitor<ProposalTagOptions> proposalTagOptionsMonitor, ILogger<ProposalProvider> logger)
    {
        _objectMapper = objectMapper;
        _proposalProvider = proposalProvider;
        _voteProvider = voteProvider;
        _proposalTagOptionsMonitor = proposalTagOptionsMonitor;
        _logger = logger;
        _DAOProvider = DAOProvider;
        _proposalAssistService = proposalAssistService;
    }

    public async Task<PagedResultDto<ProposalListDto>> QueryProposalListAsync(QueryProposalListInput input)
    {
        var tuple = await _proposalProvider.GetProposalListAsync(input);

        if (tuple.Item2.IsNullOrEmpty())
        {
            return new PagedResultDto<ProposalListDto>();
        }

        //query proposal vote infos
        var proposalIds = tuple.Item2.Select(item => item.ProposalId).ToList();
        var voteInfos = await _voteProvider.GetVoteInfosAsync(input.ChainId, proposalIds);
        var resultList = new List<ProposalListDto>();
        foreach (var proposal in tuple.Item2)
        {
            var proposalDto = _objectMapper.Map<ProposalIndex, ProposalListDto>(proposal);

            if (voteInfos.TryGetValue(proposal.ProposalId, out var voteInfo))
            {
                _objectMapper.Map(voteInfo, proposalDto);
            }

            resultList.Add(proposalDto);
        }
        foreach (var listDto in resultList.Where(listDto => listDto.ProposalType == ProposalType.Advisory.ToString()))
        {
            listDto.ExecuteStartTime = null;
            listDto.ExecuteEndTime = null;
            listDto.ExecuteTime = null;
        }
        foreach (var listDto in resultList.Where(listDto => listDto.ProposalStatus != ProposalStatus.Executed.ToString()))
        {
            listDto.ExecuteTime = null;
        }
        return new PagedResultDto<ProposalListDto>
        {
            Items = resultList,
            TotalCount = tuple.Item1
        };
    }

    public async Task<ProposalDetailDto> QueryProposalDetailAsync(QueryProposalDetailInput input)
    {
        _logger.LogInformation("ProposalService QueryProposalDetailAsync daoid:{ProposalId} start", input.ProposalId);
        var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, input.ProposalId);
        if (proposalIndex == null)
        {
            return new ProposalDetailDto();
        }
        _logger.LogInformation("ProposalService QueryProposalDetailAsync daoid:{ProposalId} proposalIndex {proposalIndex}:", input.ProposalId, JsonConvert.SerializeObject(proposalIndex));

        var proposalDetailDto = _objectMapper.Map<ProposalIndex, ProposalDetailDto>(proposalIndex);
        proposalDetailDto.ProposalLifeList = _proposalAssistService.ConvertProposalLifeList(proposalIndex);
        var voteInfos = await _voteProvider.GetVoteInfosAsync(input.ChainId, new List<string> { input.ProposalId });
        _logger.LogInformation("ProposalService QueryProposalDetailAsync daoid:{ProposalId} voteInfos {voteInfos}:", input.ProposalId, JsonConvert.SerializeObject(voteInfos));
        if (voteInfos.TryGetValue(input.ProposalId, out var voteInfo))
        {
            _objectMapper.Map(voteInfo, proposalDetailDto);
        }

        var voteRecords = await _voteProvider.GetVoteRecordAsync(new GetVoteRecordInput
        {
            ChainId = input.ChainId,
            VotingItemId = input.ProposalId,
            Sorting = VoteTopSorting
        });
        _logger.LogInformation("ProposalService QueryProposalDetailAsync daoid:{ProposalId} voteRecords {voteRecords}:", input.ProposalId, JsonConvert.SerializeObject(voteRecords));
        proposalDetailDto.VoteTopList = _objectMapper.Map<List<IndexerVoteRecord>, List<VoteRecordDto>>(voteRecords);
        if (proposalDetailDto.ProposalType != ProposalType.Advisory.ToString())
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
        _logger.LogInformation("ProposalService QueryProposalMyInfoAsync start daoid:{DAOId}:", input.DAOId);
        var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, input.ProposalId);
        _logger.LogInformation("ProposalService QueryProposalMyInfoAsync  daoid:{DAOId}: proposalIndex:{proposalIndex}:", input.DAOId, JsonConvert.SerializeObject(proposalIndex));
        if (proposalIndex == null)
        {
            return new MyProposalDto();
        }
        var myProposalDto = _objectMapper.Map<ProposalIndex, MyProposalDto>(proposalIndex);
        _logger.LogInformation("ProposalService QueryProposalMyInfoAsync daoid:{DAOId} myProposalDto:{myProposalDto}:", input.DAOId, JsonConvert.SerializeObject(myProposalDto));
        var daoIndex = await _DAOProvider.GetAsync(new GetDAOInfoInput
        {
            ChainId = input.ChainId,
            DAOId = proposalIndex.DAOId
        });
        myProposalDto.Symbol = daoIndex?.GovernanceToken ?? string.Empty;
        //todo query real vote result, mock now
        var voteRecords = await _voteProvider.GetVoteRecordAsync(new GetVoteRecordInput
        {
            ChainId = input.ChainId,
            VotingItemId = input.ProposalId,
            Sorting = VoteTopSorting
        });
        _logger.LogInformation("ProposalService QueryProposalMyInfoAsync daoid:{DAOId} voteRecords {voteRecords}:", input.DAOId, JsonConvert.SerializeObject(voteRecords));
        // var voteStake = await _voteProvider.GetVoteStakeAsync(input.ChainId, input.ProposalId, input.Address);
        var voteStake = new IndexerVoteStake();
        if (voteRecords.Count > 0)
        {
            myProposalDto.StakeAmount = voteStake.Amount;
            myProposalDto.VotesAmount = myProposalDto.StakeAmount;
        }
        myProposalDto.CanVote = proposalIndex.ProposalStage == ProposalStage.Active;
        if (proposalIndex.ProposalStage == ProposalStage.Active)
        {
            myProposalDto.AvailableUnStakeAmount = myProposalDto.StakeAmount;
        }

        myProposalDto.ProposalIdList.Add(input.ProposalId);
        return myProposalDto;
    }

    private async Task<MyProposalDto> QueryDaoMyInfoAsync(QueryMyProposalInput input)
    {
        _logger.LogInformation("ProposalService QueryDaoMyInfoAsync daoid:{DAOId} start ", input.DAOId);
        var proposalList = await _proposalProvider.GetProposalByDAOIdAsync(input.ChainId, input.DAOId);
        _logger.LogInformation("ProposalService QueryDaoMyInfoAsync daoid:{DAOId} proposalList {proposalList}:", input.DAOId, JsonConvert.SerializeObject(proposalList));
        var myProposalDto = new MyProposalDto
        {
            ChainId = input.ChainId
        };
        if (proposalList.IsNullOrEmpty())
        {
            return myProposalDto;
        }
        var daoIndex = await _DAOProvider.GetAsync(new GetDAOInfoInput
        {
            ChainId = input.ChainId,
            DAOId = input.DAOId
        });
        _logger.LogInformation("ProposalService QueryDaoMyInfoAsync daoid:{DAOId} daoIndex {daoIndex}:", input.DAOId, JsonConvert.SerializeObject(daoIndex));
        myProposalDto.Symbol = daoIndex?.GovernanceToken ?? string.Empty;
        myProposalDto.CanVote = false;
        foreach (var proposalIndex in proposalList)
        {
            var voteRecords = await _voteProvider.GetVoteRecordAsync(new GetVoteRecordInput
            {
                ChainId = input.ChainId,
                VotingItemId = proposalIndex.ProposalId,
                Sorting = VoteTopSorting
            });
            _logger.LogInformation("ProposalService QueryDaoMyInfoAsync daoid:{DAOId} voteRecords {voteRecords}:", input.DAOId, JsonConvert.SerializeObject(voteRecords));
            if (voteRecords.Count > 0)
            {
                _logger.LogInformation("ProposalService QueryDaoMyInfoAsync daoid:{DAOId} in count", input.DAOId);
                myProposalDto.StakeAmount += voteRecords[0].Amount;
                myProposalDto.VotesAmount += myProposalDto.StakeAmount;
            }
            _logger.LogInformation("ProposalService QueryDaoMyInfoAsync daoid:{DAOId} out count", input.DAOId);
            myProposalDto.ProposalIdList.Add(proposalIndex.ProposalId);
            _logger.LogInformation("ProposalService QueryDaoMyInfoAsync daoid:{DAOId} out1 count", input.DAOId);
            //todo query real vote result, mock now
            // var voteStake = await _voteProvider.GetVoteStakeAsync(input.ChainId, input.ProposalId, input.Address);
            var voteStake = new IndexerVoteStake();
            if (proposalIndex.ProposalStage == ProposalStage.Active)
            {
                myProposalDto.AvailableUnStakeAmount += myProposalDto.StakeAmount;
            }
            _logger.LogInformation("ProposalService QueryDaoMyInfoAsync daoid:{DAOId} out2 count", input.DAOId);
        }
        _logger.LogInformation("ProposalService QueryDaoMyInfoAsync daoid:{DAOId} end myProposalDto {myProposalDto}:", input.DAOId, JsonConvert.SerializeObject(myProposalDto));
        return myProposalDto;
    }

    private async Task<VoteHistoryDto>  QueryVoteRecordsAsync(QueryVoteHistoryInput input)
    {
        _logger.LogInformation("ProposalService QueryVoteRecordsAsync daoid:{DAOId} start ", input.DAOId);
        var myProposalDto = new VoteHistoryDto
        {
            ChainId = input.ChainId
        };
        var voteRecords = await _voteProvider.GetVoteRecordAsync(new GetVoteRecordInput
        {
            ChainId = input.ChainId,
            VotingItemId = input.ProposalId,
            Sorting = VoteTopSorting,
            Voter = input.Address
        });
        _logger.LogInformation("ProposalService QueryVoteRecordsAsync daoid:{DAOId} voteRecords:{voteRecords} ", input.DAOId, JsonConvert.SerializeObject(voteRecords));
        var votingItemIds = new List<string> { };
        foreach (var voteRecordItem in voteRecords )
        {
            var indexerVoteHistory = new IndexerVoteHistoryDto
            {
                TimeStamp = voteRecordItem.VoteTime,
                ProposalId = voteRecordItem.VotingItemId,
                MyOption = voteRecordItem.Option,
                VoteNum = voteRecordItem.Amount,
                TransactionId = voteRecordItem.TransactionId,
            };
            votingItemIds.Add(voteRecordItem.VotingItemId);
            myProposalDto.Items.Add(indexerVoteHistory);
        }
        var voteInfos = await _voteProvider.GetVoteInfosAsync(input.ChainId, votingItemIds);
        _logger.LogInformation("ProposalService QueryVoteRecordsAsync daoid:{DAOId} voteInfos:{voteInfos} ", input.DAOId, JsonConvert.SerializeObject(voteInfos));
        foreach (var voteRecordItem in voteRecords)
        {
            var indexerVoteHistory = new IndexerVoteHistoryDto
            {
                TimeStamp = voteRecordItem.VoteTime,
                ProposalId = voteRecordItem.VotingItemId,
                MyOption = voteRecordItem.Option,
                VoteNum = voteRecordItem.Amount,
                TransactionId = voteRecordItem.TransactionId,
            };
            // votingItemIds.Add(voteRecordItem.VotingItemId);
            if (voteInfos.TryGetValue(voteRecordItem.VotingItemId, out var voteInfo))
            {
                indexerVoteHistory.Executer = voteInfo.Executer;
            }
            var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, voteRecordItem.VotingItemId);
            if (proposalIndex == null)
            {
                indexerVoteHistory.ProposalTitle = proposalIndex.ProposalTitle;
            }
            _logger.LogInformation("ProposalService QueryVoteRecordsAsync daoid:{DAOId} proposalIndex:{proposalIndex} ", input.DAOId, JsonConvert.SerializeObject(proposalIndex));
            
            myProposalDto.Items.Add(indexerVoteHistory);
        }
        return myProposalDto;
    }

    public async Task<VoteHistoryDto> QueryVoteHistoryAsync(QueryVoteHistoryInput input)
    {
        return await QueryVoteRecordsAsync(input);
    }
}