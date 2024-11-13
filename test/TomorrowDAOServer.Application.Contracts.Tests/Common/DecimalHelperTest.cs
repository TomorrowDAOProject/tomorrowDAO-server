using Shouldly;
using TomorrowDAOServer.Common;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Application.Contracts.Tests.Common;

public class DecimalHelperTest : TomorrowDaoServerApplicationContractsTestsBase
{
    public DecimalHelperTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void ToStringTest()
    {
        var value = new decimal(10.25445);
        var str = value.ToString(2);
        str.ShouldBe("10.25");
        
        value = new decimal(10.25645);
        str = value.ToString(2);
        str.ShouldBe("10.26");
        
        value = new decimal(10.25645);
        str = value.ToString(2, DecimalHelper.RoundingOption.Ceiling);
        str.ShouldBe("10.26");
        
        value = new decimal(10.25645);
        str = value.ToString(2, DecimalHelper.RoundingOption.Floor);
        str.ShouldBe("10.25");
        
        value = new decimal(10.25645);
        str = value.ToString(2, DecimalHelper.RoundingOption.Truncate);
        str.ShouldBe("10.25");
    }
}