using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace TomorrowDAOServer.Monitor.Logging;

public static class MonitorLogHelper
{
    public static ILogger CreateLogger(IOptionsMonitor<MonitorForLoggingOptions> options)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .Enrich.FromLogContext()
#if DEBUG
            .WriteTo.Async(c =>
                c.Console(
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{Offset:zzz}][{Level:u3}] {Message}{NewLine}"))
#endif
            .WriteTo.Async(c =>
                c.File(
                    options.CurrentValue.LogFilePathFormat,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: options.CurrentValue.LogRetainedFileCountLimit,
                    outputTemplate:
                    "{Message}{NewLine}"))
            .CreateLogger();
    }
}