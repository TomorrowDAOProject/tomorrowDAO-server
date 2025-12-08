using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using TomorrowDAOServer.Monitor.Http;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Application.Contracts.Tests.Monitor.Http;

public partial class PerformanceMonitorMiddlewareTest
{
    private RequestDelegate MockRequestDelegate()
    {
        var mock = new Mock<RequestDelegate>();

        return mock.Object;
    }

    private IOptionsMonitor<PerformanceMonitorMiddlewareOptions> MockPerformanceMonitorMiddlewareOptions()
    {
        var mock = new Mock<IOptionsMonitor<PerformanceMonitorMiddlewareOptions>>();

        mock.Setup(o => o.CurrentValue).Returns(new PerformanceMonitorMiddlewareOptions());

        return mock.Object;
    }
    
    
    // Configure<PerformanceMonitorMiddlewareOptions>(configuration.GetSection("PerformanceMonitorMiddleware"));
    // Configure<MonitorForLoggingOptions>(configuration.GetSection("MonitorForLoggingOptions"));
}