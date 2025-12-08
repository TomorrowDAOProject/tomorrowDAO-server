using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Chains;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Common;

public class ChainAppServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IChainAppService _chainAppService;

    public ChainAppServiceTest(ITestOutputHelper output) : base(output)
    {
        _chainAppService = Application.ServiceProvider.GetRequiredService<IChainAppService>();
    }

    [Fact]
    public async Task GetListAsyncTest()
    {
        var list = await _chainAppService.GetListAsync();
        list.ShouldNotBeNull();
        list.ShouldNotBeEmpty();
        list.Length.ShouldBe(1);
        list[0].ShouldBe(ChainIdtDVW);
    }

    [Fact]
    public async Task GetChainIdAsyncTest()
    {
        var chainId = await _chainAppService.GetChainIdAsync(0);
        chainId.ShouldNotBeNull();
        chainId.ShouldBe(ChainIdtDVW);
    }
    
    [Fact]
    public async Task GetChainIdAsyncTest_InvalidIndex()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _chainAppService.GetChainIdAsync(10);
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldContain("ChainId at index");
    }
    
}