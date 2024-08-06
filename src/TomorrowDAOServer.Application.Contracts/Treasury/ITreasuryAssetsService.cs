using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Treasury.Dto;

namespace TomorrowDAOServer.Treasury;

public interface ITreasuryAssetsService
{
    Task<TreasuryAssetsPagedResultDto> GetTreasuryAssetsAsync(GetTreasuryAssetsInput input);
    Task<bool> IsTreasuryDepositorAsync(IsTreasuryDepositorInput input);
    Task<string> GetTreasuryAddressAsync(GetTreasuryAddressInput input);
}