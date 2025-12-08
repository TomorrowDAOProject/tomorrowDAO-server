using System.Collections.Generic;
using Nest;

namespace TomorrowDAOServer.Dtos.NetworkDao;

public class TreasuryBalanceRequest
{
    public string ChainId { get; set; } 
}


public class TreasuryBalanceResponse
{
    public string ContractAddress { get; set; }
    public List<BalanceItem> Items { get; set; }

    public class BalanceItem
    {
        public string TotalCount { get; set; }
        public string DollarValue { get; set; }
        public TokenDto Token { get; set; }
    }

    
}