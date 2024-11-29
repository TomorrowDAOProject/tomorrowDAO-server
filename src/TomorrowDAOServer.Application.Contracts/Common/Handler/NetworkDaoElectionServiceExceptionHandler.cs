using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleGetBpVotingStakingAmountAsync(Exception e, long lastQueryAmount, long lastUpdateTime)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = lastQueryAmount
        };
    }
}