using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.User;

public interface IUserAppService
{
    Task CreateUserAsync(UserDto user);
    Task<UserDto> GetUserByIdAsync(string userId);
    Task<List<UserIndex>> GetUserByCaHashListAsync(List<string> caHashes);
}