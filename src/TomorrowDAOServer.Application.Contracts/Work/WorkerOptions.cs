using System.Collections.Generic;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Work;

public class WorkerOptions
{ 
    public Dictionary<string, WorkerSetting> WorkerSettings { get; set; }
    
    public WorkerSetting GetWorkerSettings(WorkerBusinessType businessType)
    {
        return WorkerSettings?.GetValueOrDefault(businessType.ToString()) ?? 
               new WorkerSetting();
    }
}

public class WorkerSetting
{ 
    public int TimePeriod { get; set; } = 3000;

    public bool OpenSwitch { get; set; } = true;
}