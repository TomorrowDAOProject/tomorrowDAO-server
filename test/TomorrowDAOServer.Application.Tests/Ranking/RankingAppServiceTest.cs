using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Ranking.Dto;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Ranking;

public partial class RankingAppServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IRankingAppService _rankingAppService;
    
    public RankingAppServiceTest(ITestOutputHelper output) : base(output)
    {
        _rankingAppService = ServiceProvider.GetRequiredService<IRankingAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockRankingOptions());
        services.AddSingleton(MockTelegramAppsProvider());
        services.AddSingleton(MockRankingAppProvider());
        services.AddSingleton(MockUserProvider());
        services.AddSingleton(MockDAOProvider());
    }

    [Fact]
    public async Task GenerateRankingAppTest()
    {
        await _rankingAppService.GenerateRankingApp(new List<IndexerProposal>
        {
            new()
            {
                ProposalId = ProposalId1, ProposalDescription = "##GameRanking:crypto-bot"
            }
        });
    }

    [Fact]
    public async Task GetDefaultRankingProposalAsyncTest()
    {
        await _rankingAppService.GetDefaultRankingProposalAsync(ChainIdTDVV);
    }
    
    [Fact]
    public async Task GetRankingProposalListAsyncTest()
    {
        await _rankingAppService.GetRankingProposalListAsync(new GetRankingListInput{ChainId = ChainIdTDVV});
    }
    
    [Fact]
    public async Task GetRankingProposalDetailAsyncTest()
    {
        await _rankingAppService.GetRankingProposalDetailAsync(ChainIdTDVV, ProposalId1, DAOId);
        await _rankingAppService.GetRankingProposalDetailAsync(ChainIdTDVV, ProposalId2, DAOId);
        await _rankingAppService.GetRankingProposalDetailAsync(ChainIdTDVV, ProposalId3, DAOId);
    }
}