namespace TomorrowDAOServer.Grains.State.Users;

[GenerateSerializer]
public class UserTaskState
{
    [Id(0)] public DateTime CompleteTime { get; set; }
}