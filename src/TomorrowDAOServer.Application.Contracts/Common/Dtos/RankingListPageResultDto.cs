namespace TomorrowDAOServer.Common.Dtos;

public class RankingListPageResultDto<T>  : PageResultDto<T>
{
    public long UserTotalPoints { get; set; }
}