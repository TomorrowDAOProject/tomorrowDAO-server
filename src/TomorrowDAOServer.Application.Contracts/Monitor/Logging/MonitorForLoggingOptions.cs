namespace TomorrowDAOServer.Monitor.Logging;

public class MonitorForLoggingOptions
{
    public bool DisableLogging { get; set; } = false;

    public string LogFilePathFormat { get; set; } = "./Logs/monitor/trace-.log";

    public int LogRetainedFileCountLimit { get; set; } = 30;

    public int MetricNameMaxLength { get; set; } = 100;
}