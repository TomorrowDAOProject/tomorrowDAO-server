namespace TomorrowDAOServer.Grains.State.Users;

[GenerateSerializer]
public class UserViewAdTimeStampState
{
    [Id(0)] public long TimeStamp { get; set; }
    [Id(1)] public long DailyViewAdCount { get; set; }
    [Id(2)] public DateTime LastUpdateDate { get; set; }
}