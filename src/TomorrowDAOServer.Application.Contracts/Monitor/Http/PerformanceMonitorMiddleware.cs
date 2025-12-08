using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common.Handler;
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
        await InvokeTrackAsync(_monitor, context, stopwatch);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleInvokeAsync))]
    public virtual async Task InvokeTrackAsync(IMonitor monitor, HttpContext context, Stopwatch stopwatch)
    {
        await _next(context);
        await Track(monitor, context, stopwatch, false);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, Message = "error recording http request")]
    public static async Task Track(IMonitor monitor, HttpContext context, Stopwatch stopwatch, bool isException)
    {
        if (monitor == null)
        {
            return;
        }

        stopwatch.Stop();
        var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        var path = context.Request.Path;
        IDictionary<string, string> properties = new Dictionary<string, string>()
        {
            { MonitorConstant.LabelSuccess, (!isException).ToString() }
        };
        monitor.TrackMetric(chart: MonitorConstant.Api, type: path, duration: elapsedMs, properties: properties);
    }
}