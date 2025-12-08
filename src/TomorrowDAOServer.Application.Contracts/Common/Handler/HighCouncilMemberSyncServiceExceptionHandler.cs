using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleGetCandidateElectedDaoId(Exception e, ISet<string> daoIds,
        string chainId, long lastEndHeight, long newIndexHeight)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = newIndexHeight
        };
    }

    public static async Task<FlowBehavior> HandleGetHighCouncilConfigChangedDaoId(Exception e, ISet<string> daoIds,
        string chainId, long lastEndHeight, long newIndexHeight)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = newIndexHeight
        };
    }
}