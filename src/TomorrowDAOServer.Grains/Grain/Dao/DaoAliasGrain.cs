using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.Grains.State.Dao;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Grains.Grain.Dao;

public interface IDaoAliasGrain : IGrainWithStringKey
{
    Task<GrainResultDto<int>> SaveDaoAliasInfoAsync(DaoAliasDto daoAliasDto);
    Task<GrainResultDto<List<DaoAliasDto>>> GetDaoAliasInfoAsync();
}

public class DaoAliasGrain : Grain<DaoAliasState>, IDaoAliasGrain
{
    private readonly ILogger<DaoAliasGrain> _logger;
    private readonly IObjectMapper _objectMapper;

    public DaoAliasGrain(ILogger<DaoAliasGrain> logger, IObjectMapper objectMapper)
    {
        _logger = logger;
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }
    
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    /*[ExceptionHandler(typeof(Exception), TargetType = typeof(DaoAliasGrainExceptionHandler),
        MethodName = nameof(DaoAliasGrainExceptionHandler.HandleSaveDaoAliasInfoAsync),
        Message = "Save dao alias info error", LogTargets = new []{"daoAliasDto"})]*/
    public virtual async Task<GrainResultDto<int>> SaveDaoAliasInfoAsync(DaoAliasDto daoAliasDto)
    {
        if (daoAliasDto == null)
        {
            return new GrainResultDto<int>
            {
                Message = "The parameter is null",
            };
        }
        try
        {
            if (State.DaoList.IsNullOrEmpty())
            {
                State.DaoList = new List<DaoAlias>();
            }
            
            var daoAlias = State.DaoList.Find(alias => alias.DaoId == daoAliasDto.DaoId);
            if (daoAlias != null)
            {
                return new GrainResultDto<int>
                {
                    Success = true,
                    Data = daoAlias.Serial
                };
            }

            var serial = State.DaoList.Count;
            daoAlias = _objectMapper.Map<DaoAliasDto, DaoAlias>(daoAliasDto);
            daoAlias.Serial = serial;
            daoAlias.CreateTime = DateTime.Now;
            
            State.DaoList.Add(daoAlias);
            await WriteStateAsync();
            return new GrainResultDto<int>
            {
                Success = true,
                Data = serial
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Save dao alias info error, daoAliasDto={0}",
                JsonConvert.SerializeObject(daoAliasDto));
            return new GrainResultDto<int>
            {
                Message = $"Save dao alias info error. {e.Message}",
            };
        }
    }

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(DaoAliasGrainExceptionHandler),
    //     MethodName = nameof(DaoAliasGrainExceptionHandler.HandleGetDaoAliasInfoAsync),
    //     Message = "Get dao alias info error")]
    public virtual Task<GrainResultDto<List<DaoAliasDto>>> GetDaoAliasInfoAsync()
    {
        try
        {
            var daoAliasList = State.DaoList ?? new List<DaoAlias>();

            var daoAliasDtoList = _objectMapper.Map<List<DaoAlias>, List<DaoAliasDto>>(daoAliasList);

            return Task.FromResult(new GrainResultDto<List<DaoAliasDto>>
            {
                Success = true,
                Data = daoAliasDtoList
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get dao alias info error");
            return Task.FromResult(new GrainResultDto<List<DaoAliasDto>>
            {
                Message = $"Get dao alias info error. {e.Message}"
            });
        }
    }
}