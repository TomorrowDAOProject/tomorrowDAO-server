using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Serilog;

namespace TomorrowDAOServer.Filter;

public class LoggingFilter : IActionFilter
{
    private readonly ILogger<LoggingFilter> _logger;
    private Stopwatch _stopwatch;

    public LoggingFilter(ILogger<LoggingFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        _stopwatch = Stopwatch.StartNew();
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _stopwatch.Stop();
        Log.Information($"RequestFor {context.ActionDescriptor.DisplayName} use {_stopwatch.ElapsedMilliseconds} ms");
    }
}