namespace TomorrowDAOServer.Grains.State.Discussion;

[GenerateSerializer]
public class DiscoverViewedState
{
    [Id(0)]
    public bool Viewed { get; set; }
}