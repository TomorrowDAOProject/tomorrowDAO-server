using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Governance.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.DAO;

public class DaoAppServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ILogger<DAOAppService> _logger = Substitute.For<ILogger<DAOAppService>>();
    private readonly IDAOProvider _daoProvider = Substitute.For<IDAOProvider>();
    private readonly IElectionProvider _electionProvider = Substitute.For<IElectionProvider>();
    private readonly IProposalProvider _proposalProvider = Substitute.For<IProposalProvider>();

    private readonly IGraphQLProvider _graphQlProvider = Substitute.For<IGraphQLProvider>();

    // private readonly IVoteProvider _voteProvider;
    private readonly IExplorerProvider _explorerProvider = Substitute.For<IExplorerProvider>();
    private readonly IOptionsMonitor<DaoOptions> _testDaoOptions = Substitute.For<IOptionsMonitor<DaoOptions>>();
    private readonly IGovernanceProvider _governanceProvider = Substitute.For<IGovernanceProvider>();
    private readonly IContractProvider _contractProvider = Substitute.For<IContractProvider>();
    private readonly IObjectMapper _objectMapper = Substitute.For<IObjectMapper>();
    private readonly IDAOAppService _service;
    private readonly IUserProvider _userProvider = Substitute.For<IUserProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();

    private readonly Guid userId = Guid.Parse("158ff364-3264-4234-ab20-02aaada2aaad");

    public DaoAppServiceTest(ITestOutputHelper output) : base(output)
    {
        _service = ServiceProvider.GetRequiredService<IDAOAppService>();
        // _service = new DAOAppService(_daoProvider, _electionProvider, _governanceProvider, _proposalProvider,
        //     _explorerProvider, _graphQlProvider, _objectMapper, _testDaoOptions, _contractProvider, _userProvider,
        //     _logger, _tokenService);
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(_daoProvider);
        services.AddSingleton(_electionProvider);
        services.AddSingleton(_governanceProvider);
        services.AddSingleton(_proposalProvider);
        services.AddSingleton(_explorerProvider);
        services.AddSingleton(_graphQlProvider);
        services.AddSingleton(_objectMapper);
        services.AddSingleton(_testDaoOptions);
        services.AddSingleton(_contractProvider);
        services.AddSingleton(_userProvider);
        services.AddSingleton(_logger);
        services.AddSingleton(_tokenService);
    }

    [Fact]
    public async Task GetMemberListAsyncTest()
    {
        _daoProvider.GetMemberListAsync(Arg.Any<GetMemberListInput>()).Returns(new PageResultDto<MemberDto>());
        var result = await _service.GetMemberListAsync(new GetMemberListInput
        {
            ChainId = ChainIdAELF,
            DAOId = DaoId,
            Alias = null,
            SkipCount = 0,
            MaxResultCount = 10
        });
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetMemberListAsyncTest_Alisa()
    {
        _daoProvider.GetMemberListAsync(Arg.Any<GetMemberListInput>()).Returns(new PageResultDto<MemberDto>());
        _daoProvider.GetAsync(Arg.Any<GetDAOInfoInput>()).Returns(new DAOIndex
        {
            Id = DaoId
        });
        var result = await _service.GetMemberListAsync(new GetMemberListInput
        {
            ChainId = ChainIdAELF,
            Alias = "DaoId",
            SkipCount = 0,
            MaxResultCount = 10
        });
    }

    [Fact]
    public async Task GetMemberListAsyncTest_InvalidInput()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _service.GetMemberListAsync(new GetMemberListInput
            {
                ChainId = ChainIdtDVW,
                SkipCount = 0,
                MaxResultCount = 1
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Invalid input.");
    }

    [Fact]
    public async Task GetMyDAOListAsyncTest()
    {
        Guid userId = Guid.NewGuid();
        Login(userId);

        _daoProvider.GetMyOwneredDAOListAsync(Arg.Any<QueryMyDAOListInput>(), Arg.Any<string>())
            .Returns(new Tuple<long, List<DAOIndex>>(1, new List<DAOIndex>() { new DAOIndex
                {
                    Id = DAOId,
                    ChainId = ChainIdtDVW,
                    Alias = DAOId,
                    AliasHexString = null,
                    BlockHeight = 0,
                    Creator = null,
                    Metadata = null,
                    GovernanceToken = null,
                    IsHighCouncilEnabled = false,
                    HighCouncilAddress = null,
                    HighCouncilConfig = null,
                    HighCouncilTermNumber = 0,
                    FileInfoList = null,
                    IsTreasuryContractNeeded = false,
                    SubsistStatus = false,
                    TreasuryContractAddress = null,
                    TreasuryAccountAddress = null,
                    IsTreasuryPause = false,
                    TreasuryPauseExecutor = null,
                    VoteContractAddress = null,
                    ElectionContractAddress = null,
                    GovernanceContractAddress = null,
                    TimelockContractAddress = null,
                    ActiveTimePeriod = 0,
                    VetoActiveTimePeriod = 0,
                    PendingTimePeriod = 0,
                    ExecuteTimePeriod = 0,
                    VetoExecuteTimePeriod = 0,
                    CreateTime = default,
                    IsNetworkDAO = false,
                    VoterCount = 0,
                    GovernanceMechanism = GovernanceMechanism.Referendum
                }
            }));
        var daoList = await _service.GetMyDAOListAsync(new QueryMyDAOListInput
        {
            ChainId = ChainIdtDVW,
            SkipCount = 0,
            MaxResultCount = 10,
            Type = MyDAOType.Owned
        });
        daoList.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetMyDAOListAsyncTest_NotLoggedIn()
    {
        Login(Guid.Empty);
        var daoList = await _service.GetMyDAOListAsync(new QueryMyDAOListInput
        {
            ChainId = ChainIdtDVW,
            SkipCount = 0,
            MaxResultCount = 10,
            Type = MyDAOType.Owned
        });
        daoList.ShouldBeEmpty();
    }

    //Login example
    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(userId != Guid.Empty);
        var address = userId != Guid.Empty ? "address" : null;
        _userProvider.GetAndValidateUserAddressAsync(It.IsAny<Guid>(), It.IsAny<string>()).Returns(address);
    }
}