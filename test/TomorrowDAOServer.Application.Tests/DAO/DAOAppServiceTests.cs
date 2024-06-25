using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Election.Index;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Governance.Dto;
using TomorrowDAOServer.Governance.Provider;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using Volo.Abp.Application.Dtos;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.DAO;

public class DAOAppServiceTests : TomorrowDaoServerApplicationTestBase
{
    private readonly IDAOAppService _daoAppService;

    public DAOAppServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _daoAppService = GetRequiredService<IDAOAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockDAOProvider());
        services.AddSingleton(MockElectionProvider());
        services.AddSingleton(MockProposalProvider());
        services.AddSingleton(MockGovernanceProvider());
        services.AddSingleton(MockExplorerProvider());
        services.AddSingleton(MockGraphQlProvider());
        services.AddSingleton(MockContractProvider());
        base.AfterAddApplication(services);
    }

    [Fact]
    public async void QueryDAOAsync_Test()
    {
        var result = await _daoAppService.GetDAOByIdAsync(new GetDAOInfoInput
        {
            ChainId = "AELF",
            DAOId = "test1"
        });
        result.ShouldNotBeNull();
        
        var ret = await _daoAppService.GetHcMemberListAsync(new GetHcMemberInput
        {
            ChainId = "AELF",
            DAOId = "test1",
            Type = HighCouncilType.Member.ToString()
        });
        ret.ShouldNotBeNull();
    }

    private IDAOProvider MockDAOProvider()
    {
        var mock = new Mock<IDAOProvider>();

        mock.Setup(p => p.GetSyncDAOListAsync(It.IsAny<GetChainBlockHeightInput>())).ReturnsAsync(
            new List<IndexerDAOInfo>
            {
                new()
                {
                    Id = "test1",
                    ChainId = "AELF",
                    BlockHeight = 100,
                    Creator = "AA1"
                },
                new()
                {
                    Id = "test2",
                    ChainId = "AELF",
                    BlockHeight = 100,
                    Creator = "AA2"
                }
            });

        mock.Setup(p => p.GetAsync(It.IsAny<GetDAOInfoInput>())).ReturnsAsync(
            new DAOIndex
            {
                Id = "test1",
                ChainId = "AELF",
                BlockHeight = 100,
                Creator = "AA1"
            });

        mock.Setup(p => p.GetDAOListAsync(It.IsAny<QueryDAOListInput>(), It.IsAny<ISet<string>>())).ReturnsAsync(
            new Tuple<long, List<DAOIndex>>
            (
                2,
                new List<DAOIndex>
                {
                    new()
                    {
                        Id = "test1",
                        ChainId = "AELF",
                        BlockHeight = 100,
                        Creator = "AA1",
                        GovernanceToken = "USDT"
                    },
                    new()
                    {
                        Id = "test2",
                        ChainId = "AELF",
                        BlockHeight = 100,
                        Creator = "AA2",
                        GovernanceToken = "USDT"
                    }
                }
            ));

        return mock.Object;
    }

    private IElectionProvider MockElectionProvider()
    {
        var mock = new Mock<IElectionProvider>();

        mock.Setup(p => p.GetHighCouncilListAsync(It.IsAny<GetHighCouncilListInput>())).ReturnsAsync(
            new PagedResultDto<IndexerElection>(
                1, 
                new List<IndexerElection>
                {
                    new()
                    {
                        ChainId = "AELF",
                        Address = "xxxx"
                    }
                }
            ));

        return mock.Object;
    }
    
    private IProposalProvider MockProposalProvider()
    {
        var mock = new Mock<IProposalProvider>();

        mock.Setup(p => p.GetProposalCountByDAOIds(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(1L);

        return mock.Object;
    }

    private IGovernanceProvider MockGovernanceProvider()
    {
        var mock = new Mock<IGovernanceProvider>();
        mock.Setup(p => p.GetGovernanceSchemeAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(
            new IndexerGovernanceSchemeDto
            {
                Data = new List<IndexerGovernanceScheme>
                {
                    new()
                }
            });
        return mock.Object;
    }

    private IExplorerProvider MockExplorerProvider()
    {
        var mock = new Mock<IExplorerProvider>();
        mock.Setup(p => p.GetTokenInfoAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new TokenInfoDto
        {
            Symbol = ELF,
            Holders = "222"
        });
        return mock.Object;
    }

    private IGraphQLProvider MockGraphQlProvider()
    {
        var mock = new Mock<IGraphQLProvider>();

        mock.Setup(p => p.GetHoldersAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new Dictionary<string, long>{[ELF] = 1});

        return mock.Object;
    }

    private IContractProvider MockContractProvider()
    {
        var mock = new Mock<IContractProvider>();
        mock.Setup(p => p.GetTreasuryAddressAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("Address");
        return mock.Object;
    }
}