using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using TomorrowDAOServer.Monitor.Common;

namespace TomorrowDAOServer.Monitor.Logging;

public class MonitorForLogging : IMonitor
{
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<MonitorForLoggingOptions> _monitorClientForLoggingOptions;
    private readonly IConfiguration _configuration;

    public MonitorForLogging(IOptionsMonitor<MonitorForLoggingOptions> monitorClientForLoggingOptions,
        IConfiguration configuration)
    {
        _monitorClientForLoggingOptions = monitorClientForLoggingOptions;
        _configuration = configuration;
        _logger = MonitorLogHelper.CreateLogger(monitorClientForLoggingOptions);
    }

    public void TrackMetric(string chart, string type, double duration, IDictionary<string, string>? properties = null)
    {
        TrackMetric(FormatMetric(chart, type, duration, properties));
    }

    public bool IsEnabled()
    {
        return !_monitorClientForLoggingOptions.CurrentValue.DisableLogging;
    }

    public void Flush()
    {
    }

    public void Close()
    {
    }
    
    private string FormatMetric(string chart, string type, double duration, IDictionary<string, string> properties)
    {
        var builder = new StringBuilder("{");
        builder.Append('"').Append(MonitorConstant.LabelApplication).Append('"').Append(':').Append('"').Append(MonitorConstant.Application).Append("\",");
        builder.Append('"').Append(MonitorConstant.LabelModule).Append('"').Append(':').Append('"').Append(_configuration?.GetValue<string>("ServiceName")).Append("\",");
        builder.Append('"').Append(MonitorConstant.LabelChart).Append('"').Append(':').Append('"').Append(chart).Append("\",");
        builder.Append('"').Append(MonitorConstant.LabelType).Append('"').Append(':').Append('"').Append(type).Append("\",");

        if (!properties.IsNullOrEmpty())
        {
            foreach (var property in properties)
            {
                builder.Append('"').Append(property.Key).Append('"').Append(':').Append('"').Append(property.Value).Append("\",");
            }
        }
        
        builder.Append('"').Append(MonitorConstant.LabelDuration).Append('"').Append(':').Append(duration);
        builder.Append('}');
        return builder.ToString();
    }

    private string FormatMetricName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var formatMetricName = name.Replace(".", "_");
        if (formatMetricName.Length > _monitorClientForLoggingOptions.CurrentValue.MetricNameMaxLength)
        {
            formatMetricName = formatMetricName[.._monitorClientForLoggingOptions.CurrentValue.MetricNameMaxLength];
        }

        return formatMetricName;
    }

    private void TrackMetric(string metric)
    {
        try
        {
            if (_monitorClientForLoggingOptions.CurrentValue.DisableLogging)
            {
                return;
            }

            _logger.Warning(metric);
        }
        catch (Exception e)
        {
            // No need to handle monitoring log printing exceptions.
        }
    }
}