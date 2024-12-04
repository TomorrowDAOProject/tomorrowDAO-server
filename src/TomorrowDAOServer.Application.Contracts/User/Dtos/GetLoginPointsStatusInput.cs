namespace TomorrowDAOServer.User.Dtos;

public class GetLoginPointsStatusInput
{
    public string ChainId { get; set; }
}

public class LoginPointsStatusDto
{
    public int ConsecutiveLoginDays { get; set; }
    public bool DailyLoginPointsStatus { get; set; }
    public long UserTotalPoints { get; set; }
}