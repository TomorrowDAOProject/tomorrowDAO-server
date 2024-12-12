namespace TomorrowDAOServer.Common.Dtos;

public class CurrentAppPageResultDto<T> : AccumulativeAppPageResultDto<T>
{
    public bool CanVote { get; set; }
    public long ActiveEndEpochTime { get; set; }
}