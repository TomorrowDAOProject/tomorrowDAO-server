using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Serilog;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Monitor.Common;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TomorrowDAOServer.Monitor.Orleans.Filters;

public class MethodCallFilter : IOutgoingGrainCallFilter
{
    private readonly IMonitor _monitor;
    private readonly IOptionsMonitor<MethodCallFilterOptions> _methodCallFilterOption;

    private static GrainMethodFormatter.GrainMethodFormatterDelegate _methodFormatter =
        GrainMethodFormatter.MethodFormatter;

    public MethodCallFilter(IServiceProvider serviceProvider)
    {
        _monitor = MethodFilterContext.ServiceProvider.GetService<IMonitor>();
        _methodCallFilterOption = MethodFilterContext.ServiceProvider.GetService<IOptionsMonitor<MethodCallFilterOptions>>();
        var formatterDelegate =  MethodFilterContext.ServiceProvider.GetService<GrainMethodFormatter.GrainMethodFormatterDelegate>();
        if (formatterDelegate != null)
        {
            _methodFormatter = formatterDelegate;
        }
    }

    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (!_methodCallFilterOption.CurrentValue.IsEnabled)
        {
            await context.Invoke();
            return;
        }

        if (ShouldSkip(context))
        {
            await context.Invoke();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        await InvokeAsync(_monitor, context, stopwatch);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleMethodCallInvokeAsync))]
    public virtual async Task InvokeAsync(IMonitor monitor, IOutgoingGrainCallContext context, Stopwatch stopwatch)
    {
        await context.Invoke();
        await Track(monitor, context, stopwatch, false);
    }

    private bool ShouldSkip(IOutgoingGrainCallContext context)
    {
        var grainMethod = context.InterfaceMethod;
        return grainMethod == null ||
               _methodCallFilterOption.CurrentValue.SkippedMethods.Contains(_methodFormatter(context));
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, Message = "error recording results for grain")]
    public static Task Track(IMonitor monitor, IOutgoingGrainCallContext context, Stopwatch stopwatch, bool isException)
    {
        if (monitor == null)
        {
            return Task.CompletedTask;
        }

        stopwatch.Stop();
        var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        var grainMethodName = _methodFormatter(context);
        IDictionary<string, string>? properties = new Dictionary<string, string>()
        {
            { MonitorConstant.LabelSuccess, (!isException).ToString() }
        };

        monitor.TrackMetric(MonitorConstant.Grain, grainMethodName, elapsedMs, properties);
        
        return Task.CompletedTask;
    }
}