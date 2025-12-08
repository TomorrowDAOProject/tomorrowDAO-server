using Xunit.Abstractions;

namespace TomorrowDAOServer;

public class TomorrowDAOServerGrainsTestsBase : TomorrowDAOServerOrleansTestBase<TomorrowDAOServerGrainsTestsModule>
{
    public TomorrowDAOServerGrainsTestsBase(ITestOutputHelper output) : base(output)
    {
    }
}