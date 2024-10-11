namespace TomorrowDAOServer.Grains.State.ApplicationHandler;

[GenerateSerializer]
public class BPState
{
    [Id(0)] public List<string> AddressList { get; set; }
    [Id(1)] public long Round { get; set; }
}