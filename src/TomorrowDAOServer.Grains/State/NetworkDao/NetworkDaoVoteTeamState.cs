namespace TomorrowDAOServer.Grains.State.NetworkDao;

public class NetworkDaoVoteTeamState
{
    [Id(0)] public List<NetworkDaoVoteTeam> VoteTeams { get; set; } = new();
}

[GenerateSerializer]
public class NetworkDaoVoteTeam
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string PublicKey { get; set; }
    [Id(2)] public string Address { get; set; }
    [Id(3)] public string Name { get; set; }
    [Id(4)] public string Avatar { get; set; }
    [Id(5)] public string Intro { get; set; }
    [Id(6)] public string TxId { get; set; }
    [Id(7)] public bool IsActive { get; set; }
    [Id(8)] public List<string> Socials { get; set; }
    [Id(9)] public string OfficialWebsite { get; set; }
    [Id(10)] public string Location { get; set; }
    [Id(11)] public string Mail { get; set; }
    [Id(12)] public DateTime UpdateTime { get; set; }
}

