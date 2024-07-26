using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.Grain.ApplicationHandler;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Grain.ApplicationHandler;

[CollectionDefinition(ClusterCollection.Name)]
public class BPGrainTest : TomorrowDAOServerGrainsTestsBase
{
    public BPGrainTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task SetBPAsyncTest()
    {
        var grainId = GuidHelper.GenerateId(ChainIdAELF);
        var grain = Cluster.GrainFactory.GetGrain<IBPGrain>(grainId);

        await grain.SetBPAsync(new List<string>() { Address1 }, 1);

        var bpList = await grain.GetBPAsync();
        bpList.ShouldNotBeNull();
        bpList.Count.ShouldBe(1);
        bpList[0].ShouldBe(Address1);

        var bpWithRound = await grain.GetBPWithRoundAsync();
        bpWithRound.ShouldNotBeNull();
        bpWithRound.Round.ShouldBe(1);
    }
}