using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Shouldly;
using TomorrowDAOServer.Grains.Grain.Token;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Common.Provider;

public partial class GraphQlPrividerTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IClusterClient _clusterClient;
    
    public GraphQlPrividerTest(ITestOutputHelper output) : base(output)
    {
        _graphQlProvider = Application.ServiceProvider.GetRequiredService<IGraphQLProvider>();
        _clusterClient = Application.ServiceProvider.GetRequiredService<IClusterClient>();
    }

    [Fact]
    public async Task GetTokenInfoAsyncTest()
    {
        var grain = _clusterClient.GetGrain<ITokenGrain>(GuidHelper.GenerateGrainId(ChainIdAELF, ELF));
        await grain.SetTokenInfoAsync(new TokenInfoDto
        {
            Id = "id",
            ContractAddress = "ContractAddress",
            Symbol = ELF,
            ChainId = ChainIdAELF,
            IssueChainId = null,
            TxId = null,
            Name = null,
            TotalSupply = null,
            Supply = null,
            Decimals = null,
            Holders = null,
            Transfers = null,
            LastUpdateTime = 0
        });
        
        var tokenInfo = await _graphQlProvider.GetTokenInfoAsync(ChainIdAELF, ELF);
        tokenInfo.ShouldNotBeNull();
        tokenInfo.Symbol.ShouldBe(ELF);
    }
}