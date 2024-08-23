using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Election.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Election.Provider;

public partial class ElectionProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IElectionProvider _electionProvider;

    public ElectionProviderTest(ITestOutputHelper output) : base(output)
    {
        _electionProvider = GetRequiredService<IElectionProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
    }

    [Fact]
    public async Task GetVotingItemAsyncTest()
    {
        var result = await _electionProvider.GetVotingItemAsync(new GetVotingItemInput());
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(10);
        result.Items.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].VotingItemId.ShouldBe("VotingItemId");
    }

    [Fact]
    public async Task GetVotingItemAsyncTest_Exception()
    {
        // _graphQlHelper
        //     .QueryAsync<IndexerCommonResult<ElectionPageResultDto<ElectionVotingItemDto>>>(It.IsAny<GraphQLRequest>())
        //     .Throws(new UserFriendlyException("Exception Test"));

        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _electionProvider.GetVotingItemAsync(new GetVotingItemInput
            {
                ChainId = null,
                DaoId = "ThrowException",
                SkipCount = 0,
                MaxResultCount = 0,
                StartBlockHeight = 0,
                EndBlockHeight = 0
            });
        });
    }
}