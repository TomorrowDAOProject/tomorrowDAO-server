using System.Reflection;
using AElf.Client;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common.AElfSdk;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Application.Contracts.Tests.Common.AElfSdk;

public partial class ContractProviderTest : TomorrowDaoServerApplicationContractsTestsBase
{
    private readonly IContractProvider _contractProvider;
    
    public ContractProviderTest(ITestOutputHelper output) : base(output)
    {
        _contractProvider = ServiceProvider.GetRequiredService<IContractProvider>();
        
        var type = _contractProvider.GetType();
        FieldInfo? aelfCilientField = type.GetField("_clients", BindingFlags.NonPublic | BindingFlags.Instance);
        Dictionary<string, AElfClient> dictionary = (Dictionary<string, AElfClient>)aelfCilientField.GetValue(_contractProvider);
        foreach (var aElfClient in dictionary)
        {
            FieldInfo? httpServiceField = typeof(AElfClient).GetField("_httpService", BindingFlags.NonPublic | BindingFlags.Instance);
            httpServiceField.SetValue(aElfClient.Value, MockHttpService());
        }
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
    }

    [Fact]
    public async Task ContractAddressTest()
    {
        var address = _contractProvider.ContractAddress(ChainIdAELF, "AElf.ContractNames.Treasury");
        address.ShouldNotBeNull();
        address.ShouldBe("AElfTreasuryContractAddress");
    }

    [Fact]
    public async Task ContractAddressTest_AddressNotFound()
    {
        var exception = Assert.Throws<UserFriendlyException>(() =>
        {
            _contractProvider.ContractAddress(ChainIdAELF, "TreasuryContractAddress");
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Address of contract TreasuryContractAddress on AELF not exits.");
    }

    [Fact]
    public async Task QueryTransactionResultAsyncTest()
    {
        var result = await _contractProvider.QueryTransactionResultAsync(TransactionHash.ToHex(), ChainIdAELF);
        result.ShouldNotBeNull();
        result.TransactionId.ShouldBe(TransactionHash.ToHex());
        result.Status.ShouldBe("Mined");
    }

    [Fact]
    public async Task GetTreasuryAddressAsyncTest()
    {
        var address = await _contractProvider.GetTreasuryAddressAsync(ChainIdtDVW, DaoId);
        address.ShouldNotBeNull();
        address.ShouldBe(TreasuryAddress);
    }
}