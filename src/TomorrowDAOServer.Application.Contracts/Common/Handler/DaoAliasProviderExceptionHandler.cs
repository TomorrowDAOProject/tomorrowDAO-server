using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using TomorrowDAOServer.DAO;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleGenerateDaoAliasAsync(Exception e, DAOIndex daoIndex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = daoIndex!.Id
        };
    }
}