using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
        
        await _proposalService.QueryProposalListAsync(new QueryProposalListInput
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
    }
    
}