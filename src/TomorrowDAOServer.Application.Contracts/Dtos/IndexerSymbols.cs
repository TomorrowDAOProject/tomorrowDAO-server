using System.Collections.Generic;

namespace TomorrowDAOServer.Dtos;

public class IndexerSymbols
{
    public List<SymbolInfo> TokenInfo { get; set; }
}

public class SymbolInfo
{
    public int Decimals { get; set; } 
    public string Symbol { get; set; }
    public string ChainId { get; set; }
}