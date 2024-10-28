using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Discover.Dto;

namespace TomorrowDAOServer.Discover;

public interface IDiscoverService
{
    Task<bool> DiscoverViewedAsync(string chainId);
    Task<bool> DiscoverChooseAsync(string chainId, List<string> choices);
    Task<PageResultDto<DiscoverAppDto>> GetDiscoverAppListAsync(GetDiscoverAppListInput input);
    Task<long> ViewAppAsync(ViewAppInput input);
}