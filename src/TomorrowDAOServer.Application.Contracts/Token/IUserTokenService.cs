using System.Collections.Generic;
using System.Threading.Tasks;

namespace TomorrowDAOServer.Token;

public interface IUserTokenService
{
    Task<List<UserTokenDto>> GetUserTokensAsync(string chainId);
}