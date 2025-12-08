using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Common.Provider;

public class AElfClientProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IAElfClientProvider _aElfClientProvider;
    
    public AElfClientProviderTest(ITestOutputHelper output) : base(output)
    {
        _aElfClientProvider = Application.ServiceProvider.GetRequiredService<IAElfClientProvider>();
    }

    [Fact]
    public async Task GetClientTest()
    {
        var chainName = ChainIdAELF;
        var aElfClient = _aElfClientProvider.GetClient(chainName);
        aElfClient.ShouldNotBeNull();
        
        aElfClient = _aElfClientProvider.GetClient(chainName);
        aElfClient.ShouldNotBeNull();

        Assert.Throws<KeyNotFoundException>(() =>
        {
            aElfClient = _aElfClientProvider.GetClient(ChainIdTDVV);
            aElfClient.ShouldBeNull();
        });
    }
    
}