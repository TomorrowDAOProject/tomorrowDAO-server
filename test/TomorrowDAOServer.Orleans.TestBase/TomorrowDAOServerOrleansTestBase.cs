using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Xunit.Abstractions;

namespace TomorrowDAOServer;

public abstract class
    TomorrowDAOServerOrleansTestBase : TomorrowDAOServerTestBase<TomorrowDAOServerOrleansTestBaseModule>
{
    protected readonly TestCluster Cluster;

    public TomorrowDAOServerOrleansTestBase(ITestOutputHelper output) : base(output)
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
    
    protected virtual void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
    }
}