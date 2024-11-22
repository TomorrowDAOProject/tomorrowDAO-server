using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Volo.Abp;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    
    public static async Task<FlowBehavior> HandleParseRawTransaction(Exception e, string chainId, string rawTransaction)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException("Invalid input.")
        };
    }
    
}