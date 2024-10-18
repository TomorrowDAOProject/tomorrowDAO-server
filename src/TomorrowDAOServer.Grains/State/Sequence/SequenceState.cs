namespace TomorrowDAOServer.Grains.State.Sequence;

public class SequenceState
{
    public Dictionary<string, long> CurrentValue { get; set; }
}