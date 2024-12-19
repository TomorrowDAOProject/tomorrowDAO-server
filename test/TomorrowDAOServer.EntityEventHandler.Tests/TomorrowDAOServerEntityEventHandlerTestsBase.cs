using Xunit.Abstractions;

namespace TomorrowDAOServer.EntityEventHandler.Tests;

public abstract class TomorrowDAOServerEntityEventHandlerTestsBase : TomorrowDaoServerApplicationTestBase
{
    protected TomorrowDAOServerEntityEventHandlerTestsBase(ITestOutputHelper output) : base(output)
    {
    }
}