using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserPointsRecordProvider
{
    Task BulkAddOrUpdateAsync(List<UserPointsIndex> list);
    Task GenerateTaskPointsRecordAsync(string chainId, string address, UserTaskDetail userTaskDetail, DateTime completeTime, Dictionary<string, string> information = null);
    Task GenerateTaskPointsRecordAsync(string chainId, string address, UserTaskDetail userTaskDetail, PointsType pointsType, DateTime completeTime, Dictionary<string, string> information = null);
    Task<Tuple<long, List<UserPointsIndex>>> GetPointsListAsync(GetMyPointsInput input, string address);
    Task<bool> UpdateUserTaskCompleteTimeAsync(string chainId, string address, UserTask userTask, UserTaskDetail userTaskDetail, DateTime completeTime);
    Task<List<UserPointsIndex>> GetByAddressAndUserTaskAsync(string chainId, string address, UserTask userTask);
}

public class UserPointsRecordProvider : IUserPointsRecordProvider, ISingletonDependency
{
    private readonly INESTRepository<UserPointsIndex, string> _userPointsRecordRepository;
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<UserPointsRecordProvider> _logger;

    public UserPointsRecordProvider(INESTRepository<UserPointsIndex, string> userPointsRecordRepository, 
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider, IClusterClient clusterClient, ILogger<UserPointsRecordProvider> logger)
    {
        _userPointsRecordRepository = userPointsRecordRepository;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task BulkAddOrUpdateAsync(List<UserPointsIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }

        await _userPointsRecordRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task GenerateTaskPointsRecordAsync(string chainId, string address, UserTaskDetail userTaskDetail, DateTime completeTime, Dictionary<string, string> information)
    {
        var pointsType = TaskPointsHelper.GetPointsTypeFromUserTaskDetail(userTaskDetail);
        if (pointsType == null)
        {
            return;
        }

        await GenerateTaskPointsRecordAsync(chainId, address, userTaskDetail, pointsType.Value, completeTime, information);
    }

    public async Task GenerateTaskPointsRecordAsync(string chainId, string address, UserTaskDetail userTaskDetail, PointsType pointsType,
        DateTime completeTime, Dictionary<string, string> information = null)
    {
        var userTask = TaskPointsHelper.GetUserTaskFromUserTaskDetail(userTaskDetail);
        if (userTask == null)
        {
            return;
        }

        var pointsRecordIndex = new UserPointsIndex
        {
            Id = GetId(chainId, address, userTask.Value, userTaskDetail, pointsType, completeTime, information),
            ChainId = chainId, Address = address, Information = information ?? new Dictionary<string, string>(),
            UserTask = userTask.Value, UserTaskDetail = userTaskDetail,
            PointsType = pointsType, PointsTime = completeTime,
            Points = pointsType is PointsType.Vote or PointsType.BeInviteVote or PointsType.InviteVote 
                ? _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(pointsType, 1)
                : _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(pointsType)
        };
        await _userPointsRecordRepository.AddOrUpdateAsync(pointsRecordIndex);
    }

    private string GetId(string chainId, string address, UserTask userTask, UserTaskDetail userTaskDetail, PointsType pointsType,
        DateTime completeTime, Dictionary<string, string> information = null)
    {
        switch (pointsType)
        {
            case PointsType.Vote:
                var proposalId = information?.GetValueOrDefault(CommonConstant.ProposalId) ?? string.Empty;
                return GuidHelper.GenerateGrainId(chainId, userTask, userTaskDetail, pointsType, address,
                    proposalId, completeTime.ToUtcString(TimeHelper.DatePattern));
            case PointsType.DailyFirstInvite:
            case PointsType.DailyViewAsset:
                return GuidHelper.GenerateGrainId(chainId, userTask, userTaskDetail, pointsType, address,
                    completeTime.ToUtcString(TimeHelper.DatePattern));
            case PointsType.InviteVote:
                var invitee = information?.GetValueOrDefault(CommonConstant.Invitee) ?? string.Empty;
                return GuidHelper.GenerateGrainId(chainId, userTask, userTaskDetail, pointsType, address, invitee);
            case PointsType.BeInviteVote:
                var inviter = information?.GetValueOrDefault(CommonConstant.Inviter) ?? string.Empty;
                return GuidHelper.GenerateGrainId(chainId, userTask, userTaskDetail, pointsType, address, inviter);
            case PointsType.TopInviter:
                var endTime = information?.GetValueOrDefault(CommonConstant.CycleEndTime) ?? string.Empty;
                return GuidHelper.GenerateGrainId(chainId, userTask, userTaskDetail, pointsType, address, endTime);
            case PointsType.All:
            case PointsType.Like:
            case PointsType.ExploreJoinTgChannel:
            case PointsType.ExploreFollowX:
            case PointsType.ExploreJoinDiscord:
            case PointsType.ExploreCumulateFiveInvite:
            case PointsType.ExploreCumulateTenInvite:
            case PointsType.ExploreCumulateTwentyInvite:
            default:
                return GuidHelper.GenerateGrainId(chainId, userTask, userTaskDetail, pointsType, address);
        }

    }

    public async Task<Tuple<long, List<UserPointsIndex>>> GetPointsListAsync(GetMyPointsInput input, string address)
    {
        var chainId = input.ChainId;
        var mustQuery = new List<Func<QueryContainerDescriptor<UserPointsIndex>, QueryContainer>>
        {
            q =>
                q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q =>
                q.Term(i => i.Field(t => t.Address).Value(address))
        };

        QueryContainer Filter(QueryContainerDescriptor<UserPointsIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userPointsRecordRepository.GetSortListAsync(Filter, skip: input.SkipCount, limit: input.MaxResultCount,
            sortFunc: _ => new SortDescriptor<UserPointsIndex>().Descending(index => index.PointsTime));
    }

    public async Task<bool> UpdateUserTaskCompleteTimeAsync(string chainId, string address, UserTask userTask, UserTaskDetail userTaskDetail,
        DateTime completeTime)
    {
        var id = GuidHelper.GenerateGrainId(chainId, userTask, userTaskDetail, address);
        try
        {
            var grain = _clusterClient.GetGrain<IUserTaskGrain>(id);
            return await grain.UpdateUserTaskCompleteTimeAsync(completeTime, userTask);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetUserTaskCompleteTimeAsyncException id {id}", id);
            return false;
        }
    }

    public async Task<List<UserPointsIndex>> GetByAddressAndUserTaskAsync(string chainId, string address, UserTask userTask)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserPointsIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.Address).Value(address)),
            q => q.Term(i => i.Field(t => t.UserTask).Value(userTask))
        };
        if (userTask == UserTask.Daily)
        {
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1).AddTicks(-1); 
            mustQuery.Add(q => q.DateRange(r => r
                .Field(f => f.PointsTime).GreaterThanOrEquals(todayStart).LessThanOrEquals(todayEnd)));
        }

        QueryContainer Filter(QueryContainerDescriptor<UserPointsIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _userPointsRecordRepository.GetListAsync(Filter)).Item2;
    }
}