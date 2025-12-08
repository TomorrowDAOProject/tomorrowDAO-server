using System.Collections.Generic;

namespace TomorrowDAOServer.Monitor;

public interface IMonitor
{
    public void TrackMetric(string chart, string type, double duration, IDictionary<string, string>? properties = null);

    public bool IsEnabled();

    public void Flush();

    public void Close();
}