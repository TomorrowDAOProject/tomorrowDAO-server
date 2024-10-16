using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleGetTreasuryAddressAsync(Exception e, string chainId, List<string> votingItemIds)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = string.Empty
        };
    }
}