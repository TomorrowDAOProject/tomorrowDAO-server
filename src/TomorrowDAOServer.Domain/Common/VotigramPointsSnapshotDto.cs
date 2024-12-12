using System.Collections.Generic;
using Orleans;

namespace TomorrowDAOServer.Common;

[GenerateSerializer]
public class VotigramPointsSnapshotDto
{
    public List<RedisPointSnapshotDto> RedisPointSnapshot { get; set; } = new ();
    public bool RedisDataMigrationCompleted { get; set; }
    public bool AppDataMigrationCompleted { get; set; }
}

public class RedisPointSnapshotDto
{
    public string Key { get; set; }
    public string Value { get; set; }
}