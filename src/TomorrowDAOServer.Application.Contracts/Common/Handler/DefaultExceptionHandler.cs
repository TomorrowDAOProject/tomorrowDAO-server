using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;
using Volo.Abp;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public const string DefaultReThrowMethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReThrow);
    public const string DefaultThrowMethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndThrow);
    public const string DefaultReturnMethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn);
        
    public static async Task<FlowBehavior> HandleExceptionAndReThrow(Exception ex)
    {
        Log.Error(ex, "An unexpected system exception encountered.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    public static async Task<FlowBehavior> HandleExceptionAndThrow(Exception ex)
    {
        Log.Error(ex, "An unexpected system exception encountered.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(message: "An unexpected system exception encountered.", innerException: ex)
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionAndReturn(Exception ex)
    {
        Log.Error(ex, "An unexpected system exception encountered1.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }
}