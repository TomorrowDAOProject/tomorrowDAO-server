using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;
using Volo.Abp;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleAvgPrice(Exception ex)
    {
        Log.Error(ex, "An unexpected system exception encountered1.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = 0
        };
    }
}