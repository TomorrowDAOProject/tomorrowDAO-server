using System;
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
    private readonly ITreasuryAssetsService _assetsService;

    public TreasuryAssetsServiceTest(ITestOutputHelper output) : base(output)
    {
        _assetsService = Application.ServiceProvider.GetRequiredService<TreasuryAssetsService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockTokenService());
        services.AddSingleton(ContractProviderMock.MockContractProvider());
        services.AddSingleton(GraphQlMock.MockGetTreasuryFundListResult());
    }

    [Fact]
    public async Task GetTokenInfoAsyncTest()
    {
        var tokenInfo = await _assetsService.GetTokenInfoAsync(ChainIdAELF, new HashSet<string>() { ELF });
        tokenInfo.Item1.ShouldNotBeNull();
        tokenInfo.Item1.ShouldNotBeEmpty();
        tokenInfo.Item1[ELF].Decimals.ShouldBe(8);
        tokenInfo.Item2.ShouldNotBeNull();
        tokenInfo.Item2.ShouldNotBeEmpty();
        tokenInfo.Item2[ELF].Price.ShouldBe(new decimal(ElfPrice));
    }

    [Fact]
    public async Task GetTreasuryAssetsAmountAsyncTest()
    {
        var tokenInfo = await _assetsService.GetTokenInfoAsync(ChainIdAELF, new HashSet<string>() { "ELF" });
        
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _assetsService.GetTreasuryAssetsAmountAsync(new GetTreasuryAssetsInput(), tokenInfo));
        
        var treasuryAssetsInput = new GetTreasuryAssetsInput
        {
            MaxResultCount = 100,
            SkipCount = 0,
            DaoId = "DaoId",
            ChainId = ChainIdAELF,
            Symbols = new HashSet<string>() { "ELF" } 
        };
        var treasuryAssetsAmount = await _assetsService.GetTreasuryAssetsAmountAsync(treasuryAssetsInput, tokenInfo);
        treasuryAssetsAmount.ShouldBe(ElfPrice);
    }

    [Fact]
    public async Task GetTreasuryAssetsAsyncTest()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _assetsService.GetTreasuryAssetsAsync(new GetTreasuryAssetsInput());
        });

        var treasuryAssetsInput = new GetTreasuryAssetsInput
        {
            MaxResultCount = 10,
            SkipCount = 0,
            DaoId = "DaoId",
            ChainId = ChainIdAELF,
            Symbols = new HashSet<string>() { "ELF" } 
        };
        var result = await _assetsService.GetTreasuryAssetsAsync(treasuryAssetsInput);
        result.ShouldNotBeNull();
        result.TotalUsdValue.ShouldBe(ElfPrice);
    }
}