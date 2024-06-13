using System.Collections.Generic;

namespace TomorrowDAOServer.Treasury.Dto;

public class GetTreasuryFundListResult
{
    public long Item1 { get; set; }
    public List<TreasuryFundDto> Item2 { get; set; }
}

public class TreasuryFundDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
    public string DaoId { get; set; }
    public string TreasuryAddress { get; set; }
    public string Symbol { get; set; }
    public long AvailableFunds { get; set; }
    public long LockedFunds { get; set; }
}