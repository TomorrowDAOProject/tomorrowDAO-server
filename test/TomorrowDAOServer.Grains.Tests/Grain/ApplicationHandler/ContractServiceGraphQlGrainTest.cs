using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.ApplicationHandler;
using TomorrowDAOServer.Grains.Grain.Dao;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Grain.ApplicationHandler;

[CollectionDefinition(ClusterCollection.Name)]
public class ContractServiceGraphQlGrainTest : TomorrowDAOServerGrainsTestsBase
{
    public ContractServiceGraphQlGrainTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task SetStateAsyncTest()
    {
        var grainId = GuidHelper.GenerateId(WorkerBusinessType.ProposalSync + ChainIdAELF);
        var grain = Cluster.GrainFactory.GetGrain<IContractServiceGraphQLGrain>(grainId);

        await grain.SetStateAsync(100);

        var state = await grain.GetStateAsync();
        state.ShouldBe(100);
    }
}