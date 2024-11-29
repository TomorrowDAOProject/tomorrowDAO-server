using Orleans;

namespace TomorrowDAOServer.Enums;

[GenerateSerializer]
public enum UserTask
{
    [Id(0)] None = 0,
    [Id(1)] Daily = 1,
    [Id(2)] Explore = 2
}