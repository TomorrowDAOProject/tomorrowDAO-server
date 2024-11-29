using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleIncrementInviteCountAsync(Exception e, string chainId, string address,
        long delta)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = -1
        };
    }
}