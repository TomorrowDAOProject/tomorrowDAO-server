using System;
using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class TransferTokenOption
{
    public string SenderAccount { get; set; }
    public ISet<string> SupportedSymbol { get; set; } = new HashSet<string>();

    //millisecond
    public long LockUserTimeout { get; set; } = 60000;

    //millisecond
    public long TransferTimeout { get; set; } = 60000;

    public IDictionary<string, int> SymbolDecimal { get; set; } = new Dictionary<string, int>();

    public int RetryTimes { get; set; } = 20;
    //millisecond
    public int RetryDelay { get; set; } = 3000;

    public TimeSpan GetLockUserTimeoutTimeSpan()
    {
        return TimeSpan.FromMilliseconds(LockUserTimeout);
    }

    public TimeSpan GetTransferTimeoutTimeSpan()
    {
        return TimeSpan.FromMilliseconds(TransferTimeout);
    }
}