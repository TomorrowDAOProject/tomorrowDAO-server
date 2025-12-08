using Nest;

namespace TomorrowDAOServer.Users;

public class UserAddressInfo
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
}