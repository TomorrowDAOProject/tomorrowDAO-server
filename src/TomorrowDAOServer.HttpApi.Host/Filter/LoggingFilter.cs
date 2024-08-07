using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

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
        _logger.LogInformation($"Starting request for {context.ActionDescriptor.DisplayName} at {DateTime.UtcNow}");
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _stopwatch.Stop();
        _logger.LogInformation($"Ending request for {context.ActionDescriptor.DisplayName} at {DateTime.UtcNow}");
        _logger.LogInformation($"Request for {context.ActionDescriptor.DisplayName} duration {_stopwatch.ElapsedMilliseconds} ms");
    }
}