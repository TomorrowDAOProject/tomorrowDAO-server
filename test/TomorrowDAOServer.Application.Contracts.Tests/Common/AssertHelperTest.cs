using TomorrowDAOServer.Common;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Application.Contracts.Tests.Common;

public class AssertHelperTest : TomorrowDaoServerApplicationContractsTestsBase
{
    public AssertHelperTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void AssertHelperMethodTest()
    {
        AssertHelper.IsTrue(true, "");
        AssertHelper.IsEmpty(null, "", null);
        AssertHelper.IsEmpty(Guid.Empty, "", null);
        AssertHelper.NotNull("NotNull", "", null);
        AssertHelper.NotEmpty(Guid.NewGuid(), "", null);
        AssertHelper.IsEmpty(new List<string>(), "", null);
        AssertHelper.NotEmpty(new List<string>() { "aa" }, "", null);

        Assert.Throws<UserFriendlyException>(() => { AssertHelper.IsEmpty("NotNull", "", null); });

        Assert.Throws<UserFriendlyException>(() => { AssertHelper.IsEmpty(Guid.NewGuid(), "", null); });

        Assert.Throws<UserFriendlyException>(() => { AssertHelper.NotNull(null, "", null); });

        Assert.Throws<UserFriendlyException>(() => { AssertHelper.NotEmpty(Guid.Empty, "", null); });

        Assert.Throws<UserFriendlyException>(() => { AssertHelper.IsNull(new object(), "", null); });
    }
}