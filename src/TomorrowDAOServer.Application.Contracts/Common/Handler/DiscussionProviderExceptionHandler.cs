using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleGetCommentCountAsync(Exception e, string proposalId)
    {
        Log.Error(e, "GetCommentCountAsyncException proposalId {proposalId}", proposalId);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = -1
        };
    }
}