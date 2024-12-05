using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.Grains.State.Users;

[GenerateSerializer]
public class UserState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string AppId { get; set; }
    [Id(2)] public Guid UserId { get; set; }
    [Id(3)] public string UserName { get; set; }
    [Id(4)] public string CaHash { get; set; }
    [Id(5)] public List<AddressInfo> AddressInfos { get; set; }
    [Id(6)] public long CreateTime { get; set; }
    [Id(7)] public long ModificationTime { get; set; }
    [Id(8)] public string GuardianIdentifier { get; set; }
    [Id(9)] public string Address { get; set; }  //CAAddress or EOA
    [Id(10)] public string Extra { get; set; }
}