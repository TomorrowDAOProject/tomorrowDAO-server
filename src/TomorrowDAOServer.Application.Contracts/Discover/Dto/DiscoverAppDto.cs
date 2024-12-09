using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Discover.Dto;

public class DiscoverAppDto : AppDetailDto
{
    public long TotalPoints { get; set; }
    public bool Viewed { get; set; }
    public long TotalLikes { get; set; }
    public long TotalComments { get; set; }
    public long TotalOpens { get; set; }
}