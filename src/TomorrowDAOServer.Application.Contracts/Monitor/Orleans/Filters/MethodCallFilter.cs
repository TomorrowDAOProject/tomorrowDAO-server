using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using TomorrowDAOServer.Monitor.Common;

namespace TomorrowDAOServer.Monitor.Orleans.Filters;

public class MethodCallFilter : IOutgoingGrainCallFilter
{
    private readonly ILogger _logger;
    private readonly IMonitor _monitor;
    private readonly IOptionsMonitor<MethodCallFilterOptions> _methodCallFilterOption;

    private readonly GrainMethodFormatter.GrainMethodFormatterDelegate _methodFormatter =
        GrainMethodFormatter.MethodFormatter;

    public MethodCallFilter(IServiceProvider serviceProvider)
    {
        _logger = MethodFilterContext.ServiceProvider.GetService<ILogger<MethodCallFilter>>();
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
        try
        {
            await context.Invoke();
            await Track(context, stopwatch, false);
        }
        catch (Exception)
        {
            await Track(context, stopwatch, true);
            throw;
        }
    }

    private bool ShouldSkip(IOutgoingGrainCallContext context)
    {
        var grainMethod = context.InterfaceMethod;
        return grainMethod == null ||
               _methodCallFilterOption.CurrentValue.SkippedMethods.Contains(_methodFormatter(context));
    }

    private Task Track(IOutgoingGrainCallContext context, Stopwatch stopwatch, bool isException)
    {
        if (_monitor == null)
        {
            return Task.CompletedTask;
        }

        try
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var grainMethodName = _methodFormatter(context);
            IDictionary<string, string>? properties = new Dictionary<string, string>()
            {
                { MonitorConstant.LabelSuccess, (!isException).ToString() }
            };

            _monitor.TrackMetric(MonitorConstant.Grain, grainMethodName, elapsedMs, properties);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "error recording results for grain");
        }
        return Task.CompletedTask;
    }
}