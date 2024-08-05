using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Treasury.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Treasury;

public partial class TreasuryAssetsServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ITreasuryAssetsService _treasuryAssetsService;
    
    public TreasuryAssetsServiceTest(ITestOutputHelper output) : base(output)
    {
        _treasuryAssetsService = ServiceProvider.GetRequiredService<ITreasuryAssetsService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GraphQLClientMock.MockTreasuryProviderGraphQL());
        services.AddSingleton(ClusterClientMock.MockClusterClient());
        services.AddSingleton(MockDaoProvider());
        services.AddSingleton(MockNetworkDaoTreasuryService());
    }

    [Fact]
    public async Task GetTreasuryAssetsAsyncTest()
    {
       var result = await _treasuryAssetsService.GetTreasuryAssetsAsync(new GetTreasuryAssetsInput
        {
            MaxResultCount = 10,
            SkipCount = 0,
            DaoId = "DaoId",
            ChainId = ChainIdAELF,
            Symbols = new HashSet<string>() {ELF}
        });
       result.ShouldNotBeNull();
       result.DaoId.ShouldBe("DaoId");
       result.TotalCount.ShouldBe(10);
       result.TotalUsdValue.ShouldBe(0.40000000000000002);
    }
    
    [Fact]
    public async Task GetTreasuryAssetsAsyncTest_NetworkDao()
    {
        var result = await _treasuryAssetsService.GetTreasuryAssetsAsync(new GetTreasuryAssetsInput
        {
            MaxResultCount = 10,
            SkipCount = 0,
            DaoId = "DaoIdNetworkDao",
            ChainId = ChainIdAELF,
            Symbols = new HashSet<string>() {ELF}
        });
        result.ShouldNotBeNull();
        result.DaoId.ShouldBeNull();
        result.TotalCount.ShouldBe(1);
        result.TotalUsdValue.ShouldBe(50.13);
    }
    
    [Fact]
    public async Task GetTreasuryAssetsAsyncTest_InvalidInput()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _treasuryAssetsService.GetTreasuryAssetsAsync(new GetTreasuryAssetsInput());
        });
    }
    
    [Fact]
    public async Task GetTreasuryAssetsAsyncTest_GraphQLException()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _treasuryAssetsService.GetTreasuryAssetsAsync(new GetTreasuryAssetsInput
            {
                MaxResultCount = 10,
                SkipCount = 0,
                DaoId = "ThrowException",
                ChainId = ChainIdAELF,
                Symbols = new HashSet<string>()
            });
        });
        exception.Message.ShouldContain("System exception occurred during querying treasury assets");
    }

    [Fact]
    public async Task IsTreasuryDepositorAsyncTest()
    {
        var result = await _treasuryAssetsService.IsTreasuryDepositorAsync(new IsTreasuryDepositorInput
        {
            ChainId = ChainIdAELF,
            TreasuryAddress = "TreasuryAddress",
            Address = "Address",
            GovernanceToken = "ELF"
        });
        result.ShouldBeTrue();
    }
    
    [Fact]
    public async Task IsTreasuryDepositorAsyncTest_InvalidInput()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _treasuryAssetsService.IsTreasuryDepositorAsync(new IsTreasuryDepositorInput
            {
                ChainId = null,
                TreasuryAddress = null,
                Address = null,
                GovernanceToken = null
            });
        });
        exception.Message.ShouldBe("Invalid input.");
    }
    
    [Fact]
    public async Task IsTreasuryDepositorAsyncTest_GraphQLException()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _treasuryAssetsService.IsTreasuryDepositorAsync(new IsTreasuryDepositorInput
            {
                ChainId = ChainIdAELF,
                TreasuryAddress = "ThrowException",
                Address = "Address",
                GovernanceToken = "ELF"
            });
        });
        exception.Message.ShouldContain("An exception occurred when running the IsTreasuryDepositor method");
    }
}