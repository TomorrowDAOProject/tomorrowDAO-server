using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.State.Votigram;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Grains.Grain.Votigram;

public interface IVotigramSnapshotGrain : IGrainWithStringKey
{
    Task<GrainResultDto<bool>> SavePointSnapshotAsync(VotigramPointsSnapshotDto input);
    Task<GrainResultDto<VotigramPointsSnapshotDto>> GetPointSnapshotAsync();
}

public class VotigramSnapshotGrain : Grain<PointsSnapshotState>, IVotigramSnapshotGrain
{
    private readonly IObjectMapper _objectMapper;

    public VotigramSnapshotGrain(IObjectMapper objectMapper)
    {
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

    public async Task<GrainResultDto<bool>> SavePointSnapshotAsync(VotigramPointsSnapshotDto input)
    {
        if (State == null)
        {
            State = _objectMapper.Map<VotigramPointsSnapshotDto, PointsSnapshotState>(input);
        }

        if (!State.RedisDataMigrationCompleted)
        {
            State.RedisDataMigrationCompleted = input.RedisDataMigrationCompleted;
            State.RedisPointSnapshot = input.RedisPointSnapshot;
        }

        if (!State.RankingAppPointsIndexCompleted)
        {
            State.RankingAppPointsIndexCompleted = input.RankingAppPointsIndexCompleted;
            State.RankingAppPointsIndex = input.RankingAppPointsIndex;
        }

        if (!State.RankingAppUserPointsIndexCompleted)
        {
            State.RankingAppUserPointsIndexCompleted = input.RankingAppUserPointsIndexCompleted;
            State.RankingAppUserPointsIndex = input.RankingAppUserPointsIndex;
        }

        if (!State.UserPointsIndexCompleted)
        {
            State.UserPointsIndexCompleted = input.UserPointsIndexCompleted;
            State.UserPointsIndex = input.UserPointsIndex;
        }

        if (!State.UserTotalPointsCompleted)
        {
            State.UserTotalPointsCompleted = input.UserTotalPointsCompleted;
            State.UserTotalPoints = input.UserTotalPoints;
        }
        
        await WriteStateAsync();
        return new GrainResultDto<bool>
        {
            Success = true, Data = true
        };
    }

    public async Task<GrainResultDto<VotigramPointsSnapshotDto>> GetPointSnapshotAsync()
    {
        return new GrainResultDto<VotigramPointsSnapshotDto>
        {
            Success = true,
            Data = State == null ? null : _objectMapper.Map<PointsSnapshotState, VotigramPointsSnapshotDto>(State)
        };
    }
}