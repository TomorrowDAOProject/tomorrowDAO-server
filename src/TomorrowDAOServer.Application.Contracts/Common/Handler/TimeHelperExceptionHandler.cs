using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleParseFromUtc8(Exception e, string dateTimeString,
        string pattern = TimeHelper.DefaultPattern, DateTime? defaultDateTime = null)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = defaultDateTime
        };
    }
    
    public static async Task<FlowBehavior> HandleConvertStrTimeToDate(Exception e, string strTimeStamp)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = string.Empty
        };
    }
    
}