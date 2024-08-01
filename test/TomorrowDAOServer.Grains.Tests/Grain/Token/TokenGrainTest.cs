using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.Grain.Election;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Token;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Grain.Token;

public class TokenGrainTest : TomorrowDAOServerGrainsTestsBase
{
    public TokenGrainTest(ITestOutputHelper output) : base(output)
    {
    }
    
    [Fact]
    public async Task GetTokenAsyncTest()
    {
        var grainId = GuidHelper.GenerateId(ChainIdAELF, ELF);
        var grain = Cluster.GrainFactory.GetGrain<ITokenGrain>(grainId);

        var tokenExchangeDtos = await grain.GetTokenAsync(new TokenGrainDto
        {
            Id = "Id",
            ChainId = ChainIdAELF,
            Address = Address1,
            Symbol = ELF,
            Decimals = 8,
            TokenName = "AELF",
            ImageUrl = null,
            LastUpdateTime = DateTime.Now.Millisecond
        });
        tokenExchangeDtos.ShouldNotBeNull();
        
        tokenExchangeDtos = await grain.GetTokenAsync(new TokenGrainDto
        {
            Id = "Id",
            ChainId = ChainIdAELF,
            Address = Address1,
            Symbol = ELF,
            Decimals = 8,
            TokenName = "AELF",
            ImageUrl = null,
            LastUpdateTime = DateTime.Now.Millisecond
        });
        tokenExchangeDtos.ShouldNotBeNull();
    }
}