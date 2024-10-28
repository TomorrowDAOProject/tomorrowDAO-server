using Shouldly;
using TomorrowDAOServer.Common;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Application.Contracts.Tests.Common;

public class AddressHelperTest : TomorrowDaoServerApplicationContractsTestsBase
{
    public AddressHelperTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void ToFullAddressTest()
    {
        var fullAddress = AddressHelper.ToFullAddress(ChainIdAELF, Address1);
        fullAddress.ShouldNotBeNull();
        fullAddress.ShouldBe($"ELF_{Address1}_{ChainIdAELF}");
    }
    
    [Fact]
    public void FromFullAddressTest()
    {
        var (chainId, address) = AddressHelper.FromFullAddress($"ELF_{Address1}_{ChainIdAELF}");
        chainId.ShouldNotBeNull();
        chainId.ShouldBe(ChainIdAELF);

        address.ShouldNotBeNull();
        address.ShouldBe(Address1);
    }
}