using System;
using TomorrowDAOServer.Common.Dtos;

namespace TomorrowDAOServer.Treasury.Dto;

public class TreasuryAssetsPagedResultDto : TreasuryPageResultDto<TreasuryAssetsDto>
{
    public string DaoId { get; set; }
    public string TreasuryAddress { get; set; }
}

public class TreasuryAssetsDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public long Amount { get; set; }
    public int Decimal { get; set; }
    public double UsdValue { get; set; }
}