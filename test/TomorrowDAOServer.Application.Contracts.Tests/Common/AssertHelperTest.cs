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
    public Task AssertHelperMethodTest()
    {
        AssertHelper.IsTrue(true, "");
        AssertHelper.IsEmpty(null, "", null);
        AssertHelper.IsEmpty(Guid.Empty, "", null);
        AssertHelper.NotNull("NotNull", "", null);

        Assert.Throws<UserFriendlyException>(() =>
        {
            AssertHelper.IsEmpty("NotNull", "", null);
        });
        
        Assert.Throws<UserFriendlyException>(() =>
        {
            AssertHelper.IsEmpty(Guid.NewGuid(), "", null);
        });
        
        Assert.Throws<UserFriendlyException>(() =>
        {
            AssertHelper.NotNull(null, "", null);
        });
        
        return Task.CompletedTask;
    }
}