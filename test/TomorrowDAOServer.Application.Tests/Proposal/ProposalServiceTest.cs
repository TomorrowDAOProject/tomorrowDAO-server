using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Dtos.AelfScan;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Ranking.Provider;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Proposal;

public partial class ProposalServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IProposalService _proposalService;
    public ProposalServiceTest(ITestOutputHelper output) : base(output)
    {
        _proposalService = Application.ServiceProvider.GetRequiredService<IProposalService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(RankingAppPointsRedisProviderTest.MockConnectionMultiplexer());
        services.AddSingleton(RankingAppPointsRedisProviderTest.MockDistributedCache());
        services.AddSingleton(MockDaoProvider());
        services.AddSingleton(MockProposalProvider());
        services.AddSingleton(MockVoteRecordIndexRepository());
    }

    [Fact]
    public async Task QueryProposalListAsyncTest()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _proposalService.QueryProposalListAsync(new QueryProposalListInput
            {
                ChainId = ChainIdAELF
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("Invalid input");
        
        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _proposalService.QueryProposalListAsync(new QueryProposalListInput
            {
                MaxResultCount = 10,
                SkipCount = 0,
                ChainId = ChainIdAELF,
                DaoId = "notexist",
                Alias = "Alias01",
                GovernanceMechanism = null,
                ProposalType = null,
                ProposalStatus = null,
                Content = null,
                IsNetworkDao = false
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("No DAO information found");
        
        var pagedResultDto = await _proposalService.QueryProposalListAsync(new QueryProposalListInput
        {
            MaxResultCount = 10,
            SkipCount = 0,
            ChainId = ChainIdAELF,
            DaoId = "DaoId01",
            Alias = "Alias01",
            GovernanceMechanism = null,
            ProposalType = null,
            ProposalStatus = ProposalStatus.Approved,
            Content = null,
            IsNetworkDao = false
        });
        pagedResultDto.ShouldNotBeNull();
        pagedResultDto.Items.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task QueryProposalDetailAsyncTest()
    {
        var proposalDetailDto = await _proposalService.QueryProposalDetailAsync(new QueryProposalDetailInput
        {
            ChainId = ChainIdAELF,
            ProposalId = "notexist",
            Address = Address1
        });
        proposalDetailDto.ShouldNotBeNull();
        proposalDetailDto.Id.ShouldBeNull();
        
        proposalDetailDto = await _proposalService.QueryProposalDetailAsync(new QueryProposalDetailInput
        {
            ChainId = ChainIdAELF,
            ProposalId = "ProposalId",
            Address = Address1
        });
        proposalDetailDto.ShouldNotBeNull();
        proposalDetailDto.Id.ShouldNotBeNull();
        proposalDetailDto.Id.ShouldBe("Id");
    }

    [Fact]
    public async Task QueryVoteHistoryAsyncTest()
    {
        var voteHistoryPagedResultDto  = await _proposalService.QueryVoteHistoryAsync(new QueryVoteHistoryInput
        {
            ChainId = ChainIdAELF,
            ProposalId = "ProposalId",
            SkipCount = 0,
            MaxResultCount = 10
        });
        voteHistoryPagedResultDto.ShouldNotBeNull();
        voteHistoryPagedResultDto.TotalCount.ShouldBe(1);
        voteHistoryPagedResultDto.Items.ShouldNotBeEmpty();
        voteHistoryPagedResultDto.Items.First().Alias.ShouldBe("Alias");
    }

    [Fact]
    public async Task QueryExecutableProposalsAsyncTest()
    {
        var proposalPagedResultDto = await _proposalService.QueryExecutableProposalsAsync(new QueryExecutableProposalsInput
        {
            MaxResultCount = 10,
            SkipCount = 0,
            ChainId = ChainIdAELF,
            DaoId = "notexist",
            Proposer = Address1
        });
        proposalPagedResultDto.ShouldNotBeNull();
        proposalPagedResultDto.TotalCount.ShouldBe(0);
        
        proposalPagedResultDto = await _proposalService.QueryExecutableProposalsAsync(new QueryExecutableProposalsInput
        {
            MaxResultCount = 10,
            SkipCount = 0,
            ChainId = ChainIdAELF,
            DaoId = DAOId,
            Proposer = Address1
        });
        proposalPagedResultDto.ShouldNotBeNull();
        proposalPagedResultDto.TotalCount.ShouldBe(1);
        proposalPagedResultDto.Items.ShouldNotBeEmpty();
        proposalPagedResultDto.Items.First().Id.ShouldBe("Id");
    }

}