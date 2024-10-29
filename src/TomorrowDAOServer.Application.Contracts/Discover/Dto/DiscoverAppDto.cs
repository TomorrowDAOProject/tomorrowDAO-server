using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Discover.Dto;

public class DiscoverAppDto : AppDetailDto
{
    public long TotalPoints { get; set; }
    public bool Viewed { get; set; }
    public long? NotViewedNewAppCount { get; set; } = null;
}