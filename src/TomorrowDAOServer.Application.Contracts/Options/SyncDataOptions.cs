namespace TomorrowDAOServer.Options;

public class SyncDataOptions
{
    public int CacheSeconds { get; set; } = 600;
    public long RerunHeight { get; set; } = 0;
}