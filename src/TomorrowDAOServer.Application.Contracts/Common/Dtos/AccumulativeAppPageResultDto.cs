namespace TomorrowDAOServer.Common.Dtos;

public class AccumulativeAppPageResultDto<T> : PageResultDto<T>
{
    public long UserTotalPoints { get; set; }
}