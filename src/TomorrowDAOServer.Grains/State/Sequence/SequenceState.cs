namespace TomorrowDAOServer.Grains.State.Sequence;

[GenerateSerializer]
public class SequenceState
{
    [Id(0)]
    public Dictionary<string, long> CurrentValue { get; set; }
    [Id(1)]
    public long Sequence { get; set; } = 0;
}