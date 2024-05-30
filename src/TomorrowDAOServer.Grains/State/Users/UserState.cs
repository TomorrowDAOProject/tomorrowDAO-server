using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.Grains.State.Users;

public class UserState
{
    public Guid Id { get; set; }
    public string AppId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string CaHash { get; set; }
    public List<AddressInfo> AddressInfos { get; set; }
    public long CreateTime { get; set; }
    public long ModificationTime { get; set; }
}