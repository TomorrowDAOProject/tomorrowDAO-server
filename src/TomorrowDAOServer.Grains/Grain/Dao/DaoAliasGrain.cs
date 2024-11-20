using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Serilog;
using TomorrowDAOServer.Common.Handler;
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
    private readonly IObjectMapper _objectMapper;

    public DaoAliasGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    /*[ExceptionHandler(typeof(Exception), TargetType = typeof(DaoAliasGrainExceptionHandler),
        MethodName = nameof(DaoAliasGrainExceptionHandler.HandleSaveDaoAliasInfoAsync),
        Message = "Save dao alias info error", LogTargets = new []{"daoAliasDto"})]*/
    public async Task<GrainResultDto<int>> SaveDaoAliasInfoAsync(DaoAliasDto daoAliasDto)
    {
        if (daoAliasDto == null)
        {
            return new GrainResultDto<int>
            {
                Message = "The parameter is null",
            };
        }
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(DaoAliasGrainExceptionHandler),
    //     MethodName = nameof(DaoAliasGrainExceptionHandler.HandleGetDaoAliasInfoAsync),
    //     Message = "Get dao alias info error")]
    public Task<GrainResultDto<List<DaoAliasDto>>> GetDaoAliasInfoAsync()
    {
        var daoAliasList = State.DaoList ?? new List<DaoAlias>();

        var daoAliasDtoList = _objectMapper.Map<List<DaoAlias>, List<DaoAliasDto>>(daoAliasList);

        return Task.FromResult(new GrainResultDto<List<DaoAliasDto>>
        {
            Success = true,
            Data = daoAliasDtoList
        });
    }
}