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

public partial class DaoAppServiceTest : TomorrowDaoServerApplicationTestBase
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
    private readonly IDAOAppService _daoAppService;
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();

    private readonly Guid userId = Guid.Parse("158ff364-3264-4234-ab20-02aaada2aaad");

    public DaoAppServiceTest(ITestOutputHelper output) : base(output)
    {
        _daoAppService = ServiceProvider.GetRequiredService<IDAOAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(_daoProvider);
        services.AddSingleton(_electionProvider);
        services.AddSingleton(_governanceProvider);
        services.AddSingleton(_proposalProvider);
        services.AddSingleton(_explorerProvider);
        services.AddSingleton(_testDaoOptions);
        services.AddSingleton(_contractProvider);
        services.AddSingleton(_logger);
        services.AddSingleton(_tokenService);
    }

    [Fact]
    public async Task GetMemberListAsyncTest()
    {
        var environmentVariable = Environment.GetEnvironmentVariable("UNITTEST_KEY_01");
        
        _daoProvider.GetMemberListAsync(Arg.Any<GetMemberListInput>()).Returns(new PageResultDto<MemberDto>());
        var result = await _daoAppService.GetMemberListAsync(new GetMemberListInput
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
        var result = await _daoAppService.GetMemberListAsync(new GetMemberListInput
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
            await _daoAppService.GetMemberListAsync(new GetMemberListInput
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
        var userId = Guid.NewGuid();
        Login(userId);
        
        MockGetMyOwneredDAOListAsync(_daoProvider);
        MockGetMyParticipatedDaoListAsync(_daoProvider);
        MockGetManagedDAOAsync(_daoProvider);
        MockGetDaoListByDaoIds(_daoProvider);
        MockGetTokenInfoAsync(_tokenService);
        MockGetHighCouncilManagedDaoIndexAsync(_electionProvider);
        
        //All
        var daoList = await _daoAppService.GetMyDAOListAsync(new QueryMyDAOListInput
        {
            ChainId = ChainIdtDVW,
            SkipCount = 0,
            MaxResultCount = 10,
            Type = MyDAOType.All
        });
        daoList.ShouldNotBeNull();
        daoList.Count.ShouldBe(3);
        
        //Owneer
        daoList = await _daoAppService.GetMyDAOListAsync(new QueryMyDAOListInput
        {
            ChainId = ChainIdtDVW,
            SkipCount = 0,
            MaxResultCount = 10,
            Type = MyDAOType.Owned
        });
        daoList.ShouldNotBeNull();
        daoList.Count.ShouldBe(1);
        daoList[0].Type.ShouldBe(MyDAOType.Owned);
        
        //Managed
        daoList = await _daoAppService.GetMyDAOListAsync(new QueryMyDAOListInput
        {
            ChainId = ChainIdtDVW,
            SkipCount = 0,
            MaxResultCount = 10,
            Type = MyDAOType.Managed
        });
        daoList.ShouldNotBeNull();
        daoList.Count.ShouldBe(1);
        daoList[0].Type.ShouldBe(MyDAOType.Managed);
        
        //Participated
        daoList = await _daoAppService.GetMyDAOListAsync(new QueryMyDAOListInput
        {
            ChainId = ChainIdtDVW,
            SkipCount = 0,
            MaxResultCount = 10,
            Type = MyDAOType.Participated
        });
        daoList.ShouldNotBeNull();
        daoList.Count.ShouldBe(1);
        daoList[0].Type.ShouldBe(MyDAOType.Participated);
    }

    [Fact]
    public async Task GetMyDAOListAsyncTest_NotLoggedIn()
    {
        Login(Guid.Empty);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            var daoList = await _daoAppService.GetMyDAOListAsync(new QueryMyDAOListInput
            {
                ChainId = ChainIdtDVW,
                SkipCount = 0,
                MaxResultCount = 10,
                Type = MyDAOType.Owned
            });
            daoList.ShouldBeEmpty();
        });
        exception.Message.ShouldContain("No user address found");
        
    }

    [Fact]
    public async Task IsDaoMemberAsync()
    {
        MockGetMemberAsync(_daoProvider);
        var isDaoMember = await _daoAppService.IsDaoMemberAsync(new IsDaoMemberInput
        {
            ChainId = ChainIdAELF,
            DAOId = DaoId,
            MemberAddress = Address1
        });
        isDaoMember.ShouldBe(true);
    }
}