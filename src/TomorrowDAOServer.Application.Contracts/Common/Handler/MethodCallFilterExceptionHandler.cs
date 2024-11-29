using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Orleans;
using TomorrowDAOServer.Monitor;
using TomorrowDAOServer.Monitor.Orleans.Filters;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleMethodCallInvokeAsync(Exception e, IMonitor monitor,
        IOutgoingGrainCallContext context, Stopwatch stopwatch)
    {
        await MethodCallFilter.Track(monitor, context, stopwatch, true);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
}