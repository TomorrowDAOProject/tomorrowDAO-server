using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.Grains.Grain.Users;

[GenerateSerializer]
public class UserGrainDto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(2)] public string UserName { get; set; }
    [Id(3)] public string CaHash { get; set; }
    [Id(4)] public List<AddressInfo> AddressInfos { get; set; }
    [Id(5)] public long CreateTime { get; set; }
    [Id(6)] public long ModificationTime { get; set; }
}