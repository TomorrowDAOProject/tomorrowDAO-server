using Xunit.Abstractions;

namespace TomorrowDAOServer;

public class TomorrowDAOServerSiloTestBase : TomorrowDAOServerOrleansTestBase<TomorrowDAOServerOrleansTestBaseModule>
{
    public TomorrowDAOServerSiloTestBase(ITestOutputHelper output) : base(output)
    {
    }
}