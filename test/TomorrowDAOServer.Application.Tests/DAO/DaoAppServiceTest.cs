using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Governance.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.ObjectMapping;
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
    private readonly IOptionsMonitor<DaoOption> _testDaoOptions;
    private readonly IGovernanceProvider _governanceProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly DAOAppService _service;

    public DaoAppServiceTest()
    {
        _daoProvider = Substitute.For<IDAOProvider>();
        _electionProvider = Substitute.For<IElectionProvider>();
        _graphQlProvider = Substitute.For<IGraphQLProvider>();
        _proposalProvider = Substitute.For<IProposalProvider>();
        _explorerProvider = Substitute.For<IExplorerProvider>();
        _testDaoOptions = Substitute.For<IOptionsMonitor<DaoOption>>();
        _governanceProvider = Substitute.For<IGovernanceProvider>();
        _objectMapper = Substitute.For<IObjectMapper>();
        _service = new DAOAppService(_daoProvider, _electionProvider, _governanceProvider, _proposalProvider,
            _explorerProvider, _graphQlProvider, _objectMapper, _testDaoOptions);
    }

    [Fact]
    public async void GetDAOListAsync_Test()
    {
        _testDaoOptions.CurrentValue
            .Returns(new DaoOption
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
            .Returns(new List<string>{"BP"});
        _explorerProvider.GetProposalPagerAsync(Arg.Any<string>(), Arg.Any<ExplorerProposalListRequest>())
            .Returns(new ExplorerProposalResponse { Total = 1 });
        
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
}