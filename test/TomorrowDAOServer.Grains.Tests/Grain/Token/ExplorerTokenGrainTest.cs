using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.Grain.Election;
using TomorrowDAOServer.Grains.Grain.Token;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Grain.Token;

public class ExplorerTokenGrainTest : TomorrowDAOServerGrainsTestsBase
{
    public ExplorerTokenGrainTest(ITestOutputHelper output) : base(output)
    {
    }
    
    [Fact]
    public async Task SetTokenInfoAsyncTest()
    {
        var grainId = GuidHelper.GenerateId(ChainIdAELF, ELF);
        var grain = Cluster.GrainFactory.GetGrain<IExplorerTokenGrain>(grainId);

        await grain.SetTokenInfoAsync(new TokenInfoDto
        {
            Id = "id",
            ContractAddress = Address1,
            Symbol = ELF,
            ChainId = ChainIdAELF,
            IssueChainId = ChainIdAELF,
            TxId = TransactionHash.ToHex(),
            Name = "Name",
            TotalSupply = "100000000",
            Supply = "100",
            Decimals = "8",
            Holders = "Holders",
            Transfers = "Transfers",
            LastUpdateTime = DateTime.Now.Millisecond
        });

        var tokenInfoDto = await grain.GetTokenInfoAsync();
        tokenInfoDto.ShouldNotBeNull();
        tokenInfoDto.Symbol.ShouldBe(ELF);
    }
}