using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Volo.Abp.Modularity;
using Xunit.Abstractions;

namespace TomorrowDAOServer;

public abstract class TomorrowDAOServerOrleansTestBase<TStartupModule> : 
    TomorrowDAOServerTestBase<TStartupModule> where TStartupModule : IAbpModule
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