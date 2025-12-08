using System.Collections.Generic;

namespace TomorrowDAOServer.Common.Dtos;

public class PageResultDto<T>
{
    public PageResultDto()
    {
        Data = new List<T>();
        TotalCount = 0;
    }

    public PageResultDto(long totalCount, List<T> data)
    {
        Data = data;
        TotalCount = totalCount;
    }

    public List<T> Data { get; set; }
    public long TotalCount { get; set; }
}