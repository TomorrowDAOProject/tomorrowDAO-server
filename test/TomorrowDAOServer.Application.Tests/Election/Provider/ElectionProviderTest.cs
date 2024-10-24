using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Election.Index;
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

    [Fact]
    public async Task GetHighCouncilManagedDaoIndexAsyncTest()
    {
        await _electionProvider.SaveOrUpdateHighCouncilManagedDaoIndexAsync(new List<HighCouncilManagedDaoIndex>()
        {
            new HighCouncilManagedDaoIndex
            {
                Id = "Id",
                MemberAddress = Address1,
                DaoId = DAOId,
                ChainId = ChainIdAELF,
                CreateTime = DateTime.Now
            }
        });
        
        var councilManagedDaoIndices =
            await _electionProvider.GetHighCouncilManagedDaoIndexAsync(new GetHighCouncilMemberManagedDaoInput
            {
                MaxResultCount = 10,
                SkipCount = 0,
                ChainId = ChainIdAELF,
                DaoId = DAOId,
                MemberAddress = Address1
            });
        councilManagedDaoIndices.ShouldNotBeNull();
        councilManagedDaoIndices[0].MemberAddress.ShouldBe(Address1);

        await _electionProvider.DeleteHighCouncilManagedDaoIndexAsync(new List<HighCouncilManagedDaoIndex>()
        {
            new HighCouncilManagedDaoIndex
            {
                Id = "Id",
                MemberAddress = Address1,
                DaoId = DAOId,
                ChainId = ChainIdAELF,
                CreateTime = DateTime.Now
            }
        });
        
        councilManagedDaoIndices =
            await _electionProvider.GetHighCouncilManagedDaoIndexAsync(new GetHighCouncilMemberManagedDaoInput
            {
                MaxResultCount = 10,
                SkipCount = 0,
                ChainId = ChainIdAELF,
                DaoId = DAOId,
                MemberAddress = Address1
            });
        councilManagedDaoIndices.ShouldBeEmpty();

    }
}