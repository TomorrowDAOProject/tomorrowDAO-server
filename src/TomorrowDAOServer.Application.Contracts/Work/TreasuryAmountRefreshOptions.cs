using System.Collections.Generic;

namespace TomorrowDAOServer.Work;

public class TreasuryAmountRefreshOptions
{
    public Dictionary<string, ISet<string>> Symbols { get; set; }
}