using System;
using System.Collections.Generic;

namespace TomorrowDAOServer.Dtos.AelfScan;

public class GetBalanceFromAelfScanRequest
{
    public string ChainId { get; set; }
    public string Address { get; set; }
}

public class GetBalanceFromAelfScanResponse
{
    public long Total { get; set; }
    public decimal AssetInUsd { get; set; }
    public decimal AssetInElf { get; set; }
    public List<AelfScanBalanceDto> List { get; set; }
}

public class AelfScanBalanceDto
{
    public AelfScanBalanceTokenDto Token { get; set; }
    public AelfScanBalanceNftCollectionDto NftCollection { get; set; }
    public decimal Quantity { get; set; }
    public decimal ValueOfUsd { get; set; }
    public decimal PriceOfUsd { get; set; }
    public decimal PriceOfUsdPercentChange24h { get; set; }
    public decimal PriceOfElf { get; set; }
    public decimal ValueOfElf { get; set; }
    public List<string> ChainIds { get; set; }
    public string TransferCount { get; set; }
    public DateTime FirstNftTime { get; set; }
    public string Type { get; set; }
}

public class AelfScanBalanceTokenDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string ImageUrl { get; set; }
    public string Decimals { get; set; }
}

public class AelfScanBalanceNftCollectionDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string ImageUrl { get; set; }
    public string Decimals { get; set; }
}