using System.Threading.Tasks;
using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.User;

public interface IUserService
{
    Task<UserSourceReportResultDto> UserSourceReportAsync(string chainId, string source);
}