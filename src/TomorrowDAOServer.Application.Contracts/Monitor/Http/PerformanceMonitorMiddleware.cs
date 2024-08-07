using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Monitor.Common;

namespace TomorrowDAOServer.Monitor.Http;

public class PerformanceMonitorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<PerformanceMonitorMiddlewareOptions> _optionsMonitor;
    private readonly ILogger<PerformanceMonitorMiddleware> _logger;
    private readonly IMonitor _monitor;

    public PerformanceMonitorMiddleware(IServiceProvider serviceProvider, RequestDelegate next,
        IOptionsMonitor<PerformanceMonitorMiddlewareOptions> optionsMonitor,
        ILogger<PerformanceMonitorMiddleware> logger, IMonitor monitor)
    {
        _next = next;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
        _monitor = monitor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_optionsMonitor.CurrentValue.IsEnabled)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
            await Track(context, stopwatch, false);
        }
        catch (Exception)
        {
            await Track(context, stopwatch, true);
            throw;
        }
    }

    private Task Track(HttpContext context, Stopwatch stopwatch, bool isException)
    {
        if (_monitor == null)
        {
            return Task.CompletedTask;
        }

        try
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var path = context.Request.Path;
            IDictionary<string, string> properties = new Dictionary<string, string>()
            {
                { MonitorConstant.LabelSuccess, (!isException).ToString() }
            };
            _monitor.TrackMetric(chart: MonitorConstant.Api, type: path, duration: elapsedMs, properties: properties);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "error recording http request");
        }
        return Task.CompletedTask;
    }
}