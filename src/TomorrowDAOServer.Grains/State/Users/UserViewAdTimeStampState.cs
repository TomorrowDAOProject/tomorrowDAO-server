namespace TomorrowDAOServer.Grains.State.Users;

public class UserViewAdTimeStampState
{
    public long TimeStamp { get; set; }
    public long DailyViewAdCount { get; set; }
    public DateTime LastUpdateDate { get; set; }
}