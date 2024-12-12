using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Grains.State.Votigram;

[GenerateSerializer]
public class PointsSnapshotState
{
    public List<RedisPointSnapshotDto> RedisPointSnapshot { get; set; } = new ();
    public bool RedisDataMigrationCompleted { get; set; }
}