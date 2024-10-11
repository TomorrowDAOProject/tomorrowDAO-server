using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;
using Volo.Abp;

namespace TomorrowDAOServer;

public class DefaultExceptionHandler
{
    public static async Task<FlowBehavior> HandleExceptionAndThrow(UserFriendlyException ex)
    {
        Log.Error(ex, "An unexpected system exception encountered.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    public static async Task<FlowBehavior> HandleExceptionAndThrow(System.Exception ex)
    {
        Log.Error(ex, "An unexpected system exception encountered.");
        
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(message: "An unexpected system exception encountered.", innerException: ex)
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionAndReturn(System.Exception ex)
    {
        Log.Error(ex, "");

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }
}