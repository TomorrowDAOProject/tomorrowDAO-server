using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.User.Provider;
using TomorrowDAOServer.Vote;
using TomorrowDAOServer.Vote.Dto;
using TomorrowDAOServer.Vote.Index;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;
using Xunit;

namespace TomorrowDAOServer.Proposal;

public class ProposalServiceTest 
{
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
    private readonly ITokenService _tokenService;
    private readonly ProposalService _service;
    private readonly ICurrentUser _currentUser;
    private readonly IAbpLazyServiceProvider _abpLazyServiceProvider;

    public ProposalServiceTest()
    {
        _objectMapper = Substitute.For<IObjectMapper>();
        _proposalTagOptionsMonitor = Substitute.For<IOptionsMonitor<ProposalTagOptions>>();
        _proposalProvider = Substitute.For<IProposalProvider>();
        _voteProvider = Substitute.For<IVoteProvider>();
        _DAOProvider = Substitute.For<IDAOProvider>();
        _proposalAssistService = Substitute.For<IProposalAssistService>();
        _logger = Substitute.For<ILogger<ProposalProvider>>();
        _explorerProvider = Substitute.For<IExplorerProvider>();
        _graphQlProvider = Substitute.For<IGraphQLProvider>();
        _scriptService = Substitute.For<IScriptService>();
        _userProvider = Substitute.For<IUserProvider>();
        _electionProvider = Substitute.For<IElectionProvider>();
        _tokenService = Substitute.For<ITokenService>();
        _service = new ProposalService(_objectMapper, _proposalProvider, _voteProvider, 
            _graphQlProvider, _scriptService, _proposalAssistService, _DAOProvider, _proposalTagOptionsMonitor, 
            _logger, _userProvider, _electionProvider, _tokenService);
        
        _currentUser = Substitute.For<ICurrentUser>();
        _abpLazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
    }

    [Fact]
    public async void QueryProposalListAsync_Test()
    {
        _proposalProvider.GetProposalListAsync(Arg.Any<QueryProposalListInput>())
            .Returns(new Tuple<long, List<ProposalIndex>>(1, new List<ProposalIndex>
            {
                new() { ProposalId = "ProposalId1", DAOId = "DaoId", GovernanceMechanism = GovernanceMechanism.Organization, VoteSchemeId = "VoteSchemeId"}
            }));
        _objectMapper.Map<List<ProposalIndex>, List<ProposalDto>>(Arg.Any<List<ProposalIndex>>())
            .Returns(new List<ProposalDto>
            {
                new() { ProposalId = "ProposalId1", DAOId = "DaoId", GovernanceMechanism = GovernanceMechanism.Organization.ToString(), VoteSchemeId = "VoteSchemeId" }
            });
        _DAOProvider.GetMemberListAsync(Arg.Any<GetMemberListInput>()).Returns(new PageResultDto<MemberDto>
        {
            TotalCount = 10, Data = new List<MemberDto>()
        });
        _voteProvider.GetVoteItemsAsync(Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(new Dictionary<string, IndexerVote>());
        _DAOProvider.GetAsync(Arg.Any<GetDAOInfoInput>())
            .Returns(new DAOIndex { Id = "DaoId" });
        _explorerProvider.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<ExplorerTokenInfoRequest>()).Returns(new ExplorerTokenInfoResponse
        {
            Symbol = "ELF", Decimals = "8"
        });
        _voteProvider.GetVoteSchemeDicAsync(Arg.Any<GetVoteSchemeInput>())
            .Returns(new Dictionary<string, IndexerVoteSchemeInfo>());
        _tokenService.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new TokenInfoDto { Symbol = "ELF", Decimals = "8" });
        var result = await _service.QueryProposalListAsync(new QueryProposalListInput { ChainId = "AELF", DaoId = "DaoId" });
        result.ShouldNotBeNull();
    }

    [Fact]
    public async void QueryProposalMyInfoAsync_Test()
    {
        // null
        var myInfo = await _service.QueryProposalMyInfoAsync(new QueryMyProposalInput
        {
            ChainId = "AELF", DAOId = "daoId", ProposalId = "proposalId", Address = "address"
        });
        myInfo.ShouldNotBeNull();
        myInfo.Symbol.ShouldBeNull();
        
        // multi sig dao
        _proposalProvider.GetProposalByIdAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new ProposalIndex{ProposalStage = ProposalStage.Execute});
        _DAOProvider.GetAsync(Arg.Any<GetDAOInfoInput>()).Returns(new DAOIndex());
        _voteProvider.GetByVotingItemIdsAsync(Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(new List<VoteRecordIndex>());
        myInfo = await _service.QueryProposalMyInfoAsync(new QueryMyProposalInput
        {
            ChainId = "AELF", DAOId = "daoId", ProposalId = "proposalId", Address = "address"
        });
        myInfo.ShouldNotBeNull();
        myInfo.Symbol.ShouldBeNull();
        myInfo.CanVote.ShouldBe(false);
        
        // token dao
        _DAOProvider.GetAsync(Arg.Any<GetDAOInfoInput>()).Returns(new DAOIndex{GovernanceToken = "ELF"});
        _tokenService.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(new TokenInfoDto
        {
            Symbol = "ELF", Decimals = "8"
        });
        myInfo = await _service.QueryProposalMyInfoAsync(new QueryMyProposalInput
        {
            ChainId = "AELF", DAOId = "daoId", ProposalId = "proposalId", Address = "address"
        });
        myInfo.ShouldNotBeNull();
        myInfo.Symbol.ShouldBe("ELF");
        myInfo.CanVote.ShouldBe(false);
        myInfo.AvailableUnStakeAmount.ShouldBe(0);
        
        _voteProvider.GetByVoterAndVotingItemIdsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(new List<VoteRecordIndex>{new(){Amount = 10, IsWithdraw = false, EndTime = DateTime.Today.AddDays(-1)}});
        myInfo = await _service.QueryProposalMyInfoAsync(new QueryMyProposalInput
        {
            ChainId = "AELF", DAOId = "daoId", ProposalId = "proposalId", Address = "address"
        });
        myInfo.ShouldNotBeNull();
        myInfo.Symbol.ShouldBe("ELF");
        myInfo.CanVote.ShouldBe(false);
        myInfo.AvailableUnStakeAmount.ShouldBe(10);
    }
    
    [Fact]
    public async void QueryDaoMyInfoAsync_Test()
    {
        // dao is null
        var myInfo = await _service.QueryDaoMyInfoAsync(new QueryMyProposalInput
        {
            ChainId = "AELF", DAOId = "daoId", Address = "address"
        });
        myInfo.ShouldNotBeNull();
        myInfo.Symbol.ShouldBeNull();
        
        // multi sig dao
        _DAOProvider.GetAsync(Arg.Any<GetDAOInfoInput>())
            .Returns(new DAOIndex());
        _voteProvider.GetDaoVoterRecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new IndexerDAOVoterRecord());
        myInfo = await _service.QueryDaoMyInfoAsync(new QueryMyProposalInput
        {
            ChainId = "AELF", DAOId = "daoId", Address = "address"
        });
        myInfo.ShouldNotBeNull();
        myInfo.Symbol.ShouldBeNull();
        myInfo.VotesAmountUniqueVote.ShouldBe(0);
        
        // token dao
        _DAOProvider.GetAsync(Arg.Any<GetDAOInfoInput>())
            .Returns(new DAOIndex{GovernanceToken = "ELF"});
        _tokenService.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(new TokenInfoDto
        {
            Symbol = "ELF", Decimals = "8"
        });
        _voteProvider.GetNonWithdrawVoteRecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new List<VoteRecordIndex>
            {
                new() { EndTime = DateTime.Today.AddDays(1), Amount = 2 },
                new() { EndTime = DateTime.Today.AddDays(-1), Amount = 2 }
            });
        myInfo = await _service.QueryDaoMyInfoAsync(new QueryMyProposalInput
        {
            ChainId = "AELF", DAOId = "daoId", Address = "address"
        });
        myInfo.ShouldNotBeNull();
        myInfo.Symbol.ShouldBe("ELF");
    }


    [Fact]
    public async void CanExecute_Test()
    {
        var canExecute = _service.CanExecute(new ProposalDetailDto(), "address");
        canExecute.ShouldBe(false);
        
        canExecute = _service.CanExecute(new ProposalDetailDto
        {
            Proposer = "address"
        }, "address");
        canExecute.ShouldBe(false);
        
        canExecute = _service.CanExecute(new ProposalDetailDto
        {
            Proposer = "address", ProposalStatus = ProposalStatus.Approved.ToString(), ProposalStage = "Queued",
            ExecuteStartTime = DateTime.Now.AddDays(-1), ExecuteEndTime = DateTime.Now.AddDays(1)
        }, "address");
        canExecute.ShouldBe(true);
    }

    [Fact]
    public async Task QueryVoteHistoryAsync_Test()
    {
        _voteProvider.GetPageVoteRecordAsync(Arg.Any<GetPageVoteRecordInput>())
            .Returns(new Tuple<long, List<VoteRecordIndex>>(1,
                new List<VoteRecordIndex> { new() { Id = "daoId", VotingItemId = "proposalId", Amount = 100000000 } }));
        _voteProvider.GetVoteItemsAsync(Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(new Dictionary<string, IndexerVote> { ["proposalId"] = new(){Executer = "user"} });
        _proposalProvider.GetProposalByIdsAsync(Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(new List<ProposalIndex>{new() { ProposalId = "proposalId", VoteSchemeId = "82493f7880cd1d2db09ba90b85e5d5605c40db550572586185e763f75f5ede11"}});
        _DAOProvider.GetDaoListByDaoIds(Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(new List<DAOIndex> { new() { Id = "daoId" } });
        _tokenService.GetTokenInfoWithoutUpdateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new TokenInfoDto { Symbol = "ELF", Decimals = "8" });
        _objectMapper.Map<List<VoteRecordIndex>, List<IndexerVoteHistoryDto>>(Arg.Any<List<VoteRecordIndex>>())
            .Returns(new List<IndexerVoteHistoryDto> { new() { DAOId = "daoId", ProposalId = "proposalId", VoteNum = 100000000 } });
        var result = await _service.QueryVoteHistoryAsync(new QueryVoteHistoryInput
        {
            ChainId = "tDVW", DAOId = "daoId"
        });
        result.ShouldNotBeNull();
        
        _DAOProvider.GetDaoListByDaoIds(Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(new List<DAOIndex> { new() { Id = "daoId", GovernanceToken = "ELF"} });
        _proposalProvider.GetProposalByIdsAsync(Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(new List<ProposalIndex>{new() { ProposalId = "proposalId", VoteSchemeId = "934d1295190d97e81bc6c2265f74e589750285aacc2c906c7c4c3c32bd996a64"}});
        result = await _service.QueryVoteHistoryAsync(new QueryVoteHistoryInput
        {
            ChainId = "tDVW", DAOId = "daoId"
        });
        result.ShouldNotBeNull();
    }
}