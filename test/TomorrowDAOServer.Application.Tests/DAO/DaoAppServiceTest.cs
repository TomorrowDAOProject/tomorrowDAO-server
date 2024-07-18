using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
using TomorrowDAOServer.Governance.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.User.Provider;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;
using Xunit;

namespace TomorrowDAOServer.DAO;

public class DaoAppServiceTest
{
    private readonly IDAOProvider _daoProvider;
    private readonly IElectionProvider _electionProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IVoteProvider _voteProvider;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IOptionsMonitor<DaoOptions> _testDaoOptions;
    private readonly IGovernanceProvider _governanceProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly DAOAppService _service;
    private readonly IUserProvider _userProvider;
    private readonly ICurrentUser _currentUser;
    
    private readonly Guid userId = Guid.Parse("158ff364-3264-4234-ab20-02aaada2aaad");
    
    public DaoAppServiceTest()
    {
        _daoProvider = Substitute.For<IDAOProvider>();
        _electionProvider = Substitute.For<IElectionProvider>();
        _graphQlProvider = Substitute.For<IGraphQLProvider>();
        _proposalProvider = Substitute.For<IProposalProvider>();
        _explorerProvider = Substitute.For<IExplorerProvider>();
        _contractProvider = Substitute.For<IContractProvider>();
        _testDaoOptions = Substitute.For<IOptionsMonitor<DaoOptions>>();
        _governanceProvider = Substitute.For<IGovernanceProvider>();
        _objectMapper = Substitute.For<IObjectMapper>();
        _userProvider = Substitute.For<IUserProvider>();
        _currentUser = Substitute.For<ICurrentUser>();
        _service = new DAOAppService(_daoProvider, _electionProvider, _governanceProvider, _proposalProvider,
            _explorerProvider, _graphQlProvider, _objectMapper, _testDaoOptions, _contractProvider,_userProvider);
    }

    [Fact]
    public async void GetDAOListAsync_Test()
    {
        Login(userId);
        
        _testDaoOptions.CurrentValue
            .Returns(new DaoOptions
            {
                TopDaoNames = new List<string> { "Top Dao" }
            });
        _daoProvider.GetDAOListAsync(Arg.Any<QueryDAOListInput>(), Arg.Any<ISet<string>>())
            .Returns(new Tuple<long, List<DAOIndex>>(2, new List<DAOIndex>
            {
                new() { GovernanceToken = "ELF", IsNetworkDAO = false },
                new() { GovernanceToken = "USDT", IsNetworkDAO = true }
            }));
        _daoProvider.GetDAOListByNameAsync(Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(new Tuple<long, List<DAOIndex>>(1, new List<DAOIndex>
            {
                new() { GovernanceToken = "ELF", IsNetworkDAO = false },
            }));
        _objectMapper.Map<List<DAOIndex>, List<DAOListDto>>(Arg.Any<List<DAOIndex>>())
            .Returns(new List<DAOListDto>
            {
                new() { Symbol = "ELF", IsNetworkDAO = false },
                new() { Symbol = "USDT", IsNetworkDAO = true }
            });
        _explorerProvider.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new TokenInfoDto { Holders = "2" });
        _graphQlProvider.GetBPAsync(Arg.Any<string>())
            .Returns(new List<string> { "BP" });
        _explorerProvider.GetProposalPagerAsync(Arg.Any<string>(), Arg.Any<ExplorerProposalListRequest>())
            .Returns(new ExplorerProposalResponse { Total = 1 });

        _contractProvider.GetTreasuryAddressAsync(Arg.Any<string>(), Arg.Any<string>()).Returns("address");

        // begin >= topCount
        var list = await _service.GetDAOListAsync(new QueryDAOListInput
        {
            ChainId = "AELF", SkipCount = 1
        });
        list.ShouldNotBeNull();

        // end <= topCount
        list = await _service.GetDAOListAsync(new QueryDAOListInput
        {
            ChainId = "AELF", MaxResultCount = 1
        });
        list.ShouldNotBeNull();

        // both
        list = await _service.GetDAOListAsync(new QueryDAOListInput
        {
            ChainId = "AELF"
        });
        list.ShouldNotBeNull();
    }

    [Fact]
    public async void GetMemberListAsync_Test()
    {
        _daoProvider.GetMemberListAsync(Arg.Any<GetMemberListInput>()).Returns(new PageResultDto<MemberDto>());
        var result = await _service.GetMemberListAsync(new GetMemberListInput());
        result.ShouldNotBeNull();
    }
    
    //Login example
    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
        _userProvider.GetAndValidateUserAddress(It.IsAny<Guid>(), It.IsAny<string>()).Returns("address");
    }
}