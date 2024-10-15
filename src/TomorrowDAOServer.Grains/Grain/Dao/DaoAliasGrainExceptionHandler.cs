using AElf.ExceptionHandler;
using TomorrowDAOServer.DAO.Dtos;

namespace TomorrowDAOServer.Grains.Grain.Dao;

public class DaoAliasGrainExceptionHandler
{
    public static async Task<FlowBehavior> HandleSaveDaoAliasInfoAsync(Exception e, DaoAliasDto daoAliasDto)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new GrainResultDto<int>
            {
                Message = $"Save dao alias info error. {e.Message}",
            }
        };
    }
    
    public static async Task<FlowBehavior> HandleGetDaoAliasInfoAsync(Exception e, DaoAliasDto daoAliasDto)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = Task.FromResult(new GrainResultDto<List<DaoAliasDto>>
            {
                Message = $"Get dao alias info error. {e.Message}"
            })
        };
    }
}