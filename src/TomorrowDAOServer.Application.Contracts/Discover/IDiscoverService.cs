using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Discover.Dto;

namespace TomorrowDAOServer.Discover;

public interface IDiscoverService
{
    Task<bool> DiscoverViewedAsync(string chainId);
    Task<bool> DiscoverChooseAsync(string chainId, List<string> choices);
    Task<AppPageResultDto<DiscoverAppDto>> GetDiscoverAppListAsync(GetDiscoverAppListInput input);
    Task<bool> ViewAppAsync(ViewAppInput input);
}