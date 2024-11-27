using System.Collections.Generic;

namespace TomorrowDAOServer.Common.Dtos;

public class AppPageResultDto<T> : PageResultDto<T>
{
    public AppPageResultDto(long totalCount, List<T> data) : base(totalCount, data)
    {
        NotViewedNewAppCount = 0;
    }

    public AppPageResultDto(long totalCount, List<T> data, long notViewedNewAppCount) : base(totalCount, data)
    {
        NotViewedNewAppCount = notViewedNewAppCount;
    }

    public long NotViewedNewAppCount { get; set; }
}