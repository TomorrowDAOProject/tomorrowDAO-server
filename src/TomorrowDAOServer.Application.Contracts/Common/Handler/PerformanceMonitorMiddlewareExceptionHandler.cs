using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.AspNetCore.Http;
using TomorrowDAOServer.Monitor;
using TomorrowDAOServer.Monitor.Http;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleInvokeAsync(Exception e, IMonitor monitor, HttpContext context,
        Stopwatch stopwatch)
    {
        await PerformanceMonitorMiddleware.Track(monitor, context, stopwatch, true);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
}