using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.Monitor.Http;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Application.Contracts.Tests.Monitor.Http;

public partial class PerformanceMonitorMiddlewareTest : TomorrowDaoServerApplicationContractsTestsBase
{
    private readonly PerformanceMonitorMiddleware _middleware;
    public PerformanceMonitorMiddlewareTest(ITestOutputHelper output) : base(output)
    {
        _middleware = ServiceProvider.GetRequiredService<PerformanceMonitorMiddleware>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockRequestDelegate());
        services.AddSingleton(MockPerformanceMonitorMiddlewareOptions());
    }


    [Fact]
    public async Task InvokeAsyncTest()
    {
        await _middleware.InvokeAsync(new DefaultHttpContext());
    }
    
}