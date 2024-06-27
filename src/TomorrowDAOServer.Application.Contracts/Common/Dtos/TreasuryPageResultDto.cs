using System.Collections.Generic;

namespace TomorrowDAOServer.Common.Dtos;

public class TreasuryPageResultDto<T> : PageResultDto<T>
{
    public TreasuryPageResultDto()
    {
        TotalUsdValue = 0;
    }

    public TreasuryPageResultDto(long totalCount, List<T> data, double totalUsdValue) : base(totalCount, data)
    {
        TotalUsdValue = totalUsdValue;
    }

    public double TotalUsdValue { get; set; }
}