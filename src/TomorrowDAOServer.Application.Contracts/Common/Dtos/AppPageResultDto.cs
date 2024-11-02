using System.Collections.Generic;

namespace TomorrowDAOServer.Common.Dtos;

public class AppPageResultDto<T> : PageResultDto<T>
{
    public AppPageResultDto()
    {
        NotViewedNewAppCount = null;
    }

    public AppPageResultDto(long totalCount, List<T> data, long? notViewedNewAppCount) : base(totalCount, data)
    {
        NotViewedNewAppCount = notViewedNewAppCount;
    }

    public long? NotViewedNewAppCount { get; set; } = null;
}