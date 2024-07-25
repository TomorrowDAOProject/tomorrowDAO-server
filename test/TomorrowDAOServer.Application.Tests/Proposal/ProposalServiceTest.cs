using System;
using System.Collections.Generic;
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
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
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
        _service = new ProposalService(_objectMapper, _proposalProvider, _voteProvider, _explorerProvider, 
            _graphQlProvider, _scriptService, _proposalAssistService, _DAOProvider, _proposalTagOptionsMonitor, 
            _logger, _userProvider, _electionProvider);
        
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
        _explorerProvider.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new TokenInfoDto());
        _voteProvider.GetVoteSchemeDicAsync(Arg.Any<GetVoteSchemeInput>())
            .Returns(new Dictionary<string, IndexerVoteSchemeInfo>());
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
        _explorerProvider.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new TokenInfoDto{Symbol = "ELF"});
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
        _explorerProvider.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new TokenInfoDto());
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
        myInfo.Symbol.ShouldBeNull("ELF");
    }
}