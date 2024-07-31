using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.Grain.Election;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Grain.Election;

[CollectionDefinition(ClusterCollection.Name)]
public class HighCouncilMembersGrain : TomorrowDAOServerGrainsTestsBase
{
    public HighCouncilMembersGrain(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task SaveHighCouncilMembersAsyncTest()
    {
        var grainId = GuidHelper.GenerateId(ChainIdAELF, DaoId);
        var grain = Cluster.GrainFactory.GetGrain<IHighCouncilMembersGrain>(grainId);

        await grain.SaveHighCouncilMembersAsync(new List<string>() { Address1, Address2 });

        var highCouncilMembers = await grain.GetHighCouncilMembersAsync();
        highCouncilMembers.ShouldNotBeNull();
        highCouncilMembers.Count.ShouldBe(2);
        highCouncilMembers.ShouldContain(Address1);
    }
}