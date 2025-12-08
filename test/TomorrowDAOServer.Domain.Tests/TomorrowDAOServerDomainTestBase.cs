using Xunit.Abstractions;

namespace TomorrowDAOServer;

public abstract class TomorrowDAOServerDomainTestBase : TomorrowDAOServerTestBase<TomorrowDAOServerDomainTestModule>
{
    protected TomorrowDAOServerDomainTestBase(ITestOutputHelper output) : base(output)
    {
    }
}