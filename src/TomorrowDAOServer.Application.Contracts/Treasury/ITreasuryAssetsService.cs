using System.Threading.Tasks;
using TomorrowDAOServer.Treasury.Dto;

namespace TomorrowDAOServer.Treasury;

public interface ITreasuryAssetsService
{
    Task<TreasuryAssetsPagedResultDto> GetTreasuryAssetsAsync(GetTreasuryAssetsInput input);
}