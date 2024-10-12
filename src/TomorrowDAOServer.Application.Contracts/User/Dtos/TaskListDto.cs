using System.Collections.Generic;

namespace TomorrowDAOServer.User.Dtos;

public class TaskListDto
{
    public List<TaskInfo> TaskList { get; set; }
}

public class TaskInfo
{
    public long TotalCount { get; set; }
    public string UserTask { get; set; }
    public List<TaskInfoDetail> Data { get; set; }
}

public class TaskInfoDetail
{
    public long Points { get; set; }
    public string UserTaskDetail { get; set; }
    public bool Complete { get; set; }
    public long CompleteCount { get; set; }
    public long TaskCount { get; set; }
}