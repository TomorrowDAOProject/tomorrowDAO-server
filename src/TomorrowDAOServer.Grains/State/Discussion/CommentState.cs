namespace TomorrowDAOServer.Grains.State.Discussion;

[GenerateSerializer]
public class CommentState
{
    [Id(0)] public long Count { get; set; }
}