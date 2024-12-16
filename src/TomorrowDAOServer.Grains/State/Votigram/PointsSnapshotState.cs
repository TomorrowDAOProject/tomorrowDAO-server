using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Grains.State.Votigram;

[GenerateSerializer]
public class PointsSnapshotState
{
    [Id(0)] public List<RedisPointSnapshotDto> RedisPointSnapshot { get; set; } = new ();
    [Id(1)] public bool RedisDataMigrationCompleted { get; set; }
    [Id(2)]public string RankingAppPointsIndex { get; set; }
    [Id(3)] public bool RankingAppPointsIndexCompleted { get; set; }
    [Id(4)]public string RankingAppUserPointsIndex { get; set; }
    [Id(5)]public bool RankingAppUserPointsIndexCompleted { get; set; }
    [Id(6)] public string UserPointsIndex { get; set; }
    [Id(7)] public bool UserPointsIndexCompleted { get; set; }
    [Id(8)] public string UserTotalPoints { get; set; }
    [Id(9)] public bool UserTotalPointsCompleted { get; set; }
}