namespace TomorrowDAOServer.Grains.State.Election;

[GenerateSerializer]
public class HighCouncilMembersState
{
    [Id(0)] public List<string> AddressList { get; set; }
    [Id(1)] public DateTime UpdateTime { get; set; }
}