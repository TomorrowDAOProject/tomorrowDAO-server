using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Handler;
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

    Task GenerateTaskPointsRecordAsync(string chainId, string address, UserTaskDetail userTaskDetail,
        DateTime completeTime, Dictionary<string, string> information = null, string userId = "");

    Task GenerateTaskPointsRecordAsync(string chainId, string address, UserTaskDetail userTaskDetail,
        PointsType pointsType, DateTime completeTime, Dictionary<string, string> information = null,
        string userId = "");

    Task<Tuple<long, List<UserPointsIndex>>> GetPointsListAsync(GetMyPointsInput input, string address);
    Task<Tuple<long, List<UserPointsIndex>>> GetPointsListAsync(GetMyPointsInput input);

    Task<bool> UpdateUserTaskCompleteTimeAsync(string chainId, string userId, string address, UserTask userTask,
        UserTaskDetail userTaskDetail, DateTime completeTime);

    Task<List<UserPointsIndex>> GetByAddressAndUserTaskAsync(string chainId, string userId, string address,
        List<UserTask> userTasks);

    Task<bool> UpdateUserViewAdTimeStampAsync(string chainId, string userId, long timeStamp);
    Task<long> GetDailyViewAdCountAsync(string chainId, string userId);

    Task<bool> GetUserTaskCompleteAsync(string chainId, string address, UserTask userTask,
        UserTaskDetail userTaskDetail);
}

public class UserPointsRecordProvider : IUserPointsRecordProvider, ISingletonDependency
{
    private readonly INESTRepository<UserPointsIndex, string> _userPointsRecordRepository;
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<UserPointsRecordProvider> _logger;

    public UserPointsRecordProvider(INESTRepository<UserPointsIndex, string> userPointsRecordRepository,
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider, IClusterClient clusterClient,
        ILogger<UserPointsRecordProvider> logger)
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

    public async Task GenerateTaskPointsRecordAsync(string chainId, string address, UserTaskDetail userTaskDetail,
        DateTime completeTime, Dictionary<string, string> information, string userId = "")
    {
        var pointsType = TaskPointsHelper.GetPointsTypeFromUserTaskDetail(userTaskDetail);
        if (pointsType == null)
        {
            return;
        }

        await GenerateTaskPointsRecordAsync(chainId, address, userTaskDetail, pointsType.Value,
            completeTime, information, userId);
    }

    public async Task GenerateTaskPointsRecordAsync(string chainId, string address, UserTaskDetail userTaskDetail,
        PointsType pointsType,
        DateTime completeTime, Dictionary<string, string> information = null, string userId = "")
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
                : _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(pointsType),
            UserId = userId
        };
        await _userPointsRecordRepository.AddOrUpdateAsync(pointsRecordIndex);
    }

    private string GetId(string chainId, string address, UserTask userTask, UserTaskDetail userTaskDetail,
        PointsType pointsType,
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
            case PointsType.DailyViewAds:
                var timeStamp = information?.GetValueOrDefault(CommonConstant.AdTime) ?? string.Empty;
                return GuidHelper.GenerateGrainId(chainId, userTask, userTaskDetail, pointsType, address, timeStamp);
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
        return await _userPointsRecordRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount,
            sortFunc: _ => new SortDescriptor<UserPointsIndex>().Descending(index => index.PointsTime));
    }

    public async Task<Tuple<long, List<UserPointsIndex>>> GetPointsListAsync(GetMyPointsInput input)
    {
        var chainId = input.ChainId;
        var mustQuery = new List<Func<QueryContainerDescriptor<UserPointsIndex>, QueryContainer>>
        {
            q =>
                q.Term(i => i.Field(t => t.ChainId).Value(chainId))
        };

        QueryContainer Filter(QueryContainerDescriptor<UserPointsIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userPointsRecordRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount,
            sortFunc: _ => new SortDescriptor<UserPointsIndex>().Descending(index => index.PointsTime));
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, ReturnDefault = ReturnDefault.Default,
        Message = "GetUserTaskCompleteTime error",
        LogTargets = new[] { "chainId", "address", "userTask", "userTaskDetail" })]
    public virtual async Task<bool> UpdateUserTaskCompleteTimeAsync(string chainId, string userId, string address,
        UserTask userTask, UserTaskDetail userTaskDetail, DateTime completeTime)
    {
        var originalUserTask = GetOriginalUserTask(userTask);

        bool completed = false;
        if (!userId.IsNullOrWhiteSpace())
        {
            var id = GuidHelper.GenerateGrainId(chainId, originalUserTask, userTaskDetail, userId);
            var grain = _clusterClient.GetGrain<IUserTaskGrain>(id);
            completed = await grain.UpdateUserTaskCompleteTimeAsync(completeTime, originalUserTask);
        }

        if (!completed && !address.IsNullOrWhiteSpace())
        {
            var id = GuidHelper.GenerateGrainId(chainId, originalUserTask, userTaskDetail, address);
            var grain = _clusterClient.GetGrain<IUserTaskGrain>(id);
            completed = await grain.UpdateUserTaskCompleteTimeAsync(completeTime, originalUserTask);
        }

        return completed;
    }

    private UserTask GetOriginalUserTask(UserTask userTask)
    {
        if (userTask is UserTask.ExploreVotigram or UserTask.ExploreApps or UserTask.Referrals)
        {
            return UserTask.Explore;
        }

        return userTask;
    }

    public async Task<List<UserPointsIndex>> GetByAddressAndUserTaskAsync(string chainId, string userId, 
        string address, List<UserTask> userTasks)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserPointsIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Terms(i => i.Field(t => t.UserTask).Terms(userTasks))
        };
        if (userTasks.Contains(UserTask.Daily))
        {
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1).AddTicks(-1);
            mustQuery.Add(q => q.DateRange(r => r
                .Field(f => f.PointsTime).GreaterThanOrEquals(todayStart).LessThanOrEquals(todayEnd)));
            mustQuery.Add(q => !q.Term(i => i
                .Field(t => t.PointsType).Value(PointsType.DailyViewAds)));
        }
        
        var shouldQuery = new List<Func<QueryContainerDescriptor<UserPointsIndex>, QueryContainer>>();
        if (!address.IsNullOrWhiteSpace())
        {
            shouldQuery.Add(q => q.Term(i => i.Field(t => t.Address).Value(address)));
        }
        shouldQuery.Add(q => q.Term(i => i.Field(t => t.UserId).Value(userId)));

        QueryContainer Filter(QueryContainerDescriptor<UserPointsIndex> f) => f.Bool(b => b.Must(mustQuery).Should(shouldQuery).MinimumShouldMatch(1));
        return (await _userPointsRecordRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<bool> UpdateUserViewAdTimeStampAsync(string chainId, string userId, long timeStamp)
    {
        var id = GuidHelper.GenerateGrainId(chainId, userId);
        try
        {
            var grain = _clusterClient.GetGrain<IUserViewAdTimeStampGrain>(id);
            return await grain.UpdateUserViewAdTimeStampAsync(timeStamp);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UpdateUserViewAdTimeStampAsyncException id {id}", id);
            return false;
        }
    }

    public async Task<long> GetDailyViewAdCountAsync(string chainId, string userId)
    {
        var id = GuidHelper.GenerateGrainId(chainId, userId);
        try
        {
            var grain = _clusterClient.GetGrain<IUserViewAdTimeStampGrain>(id);
            return await grain.GetDailyViewAdCountAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetDailyViewAdCountAsyncException id {id}", id);
            return 0;
        }
    }

    public async Task<bool> GetUserTaskCompleteAsync(string chainId, string address, UserTask userTask,
        UserTaskDetail userTaskDetail)
    {
        var id = GuidHelper.GenerateGrainId(chainId, userTask, userTaskDetail, address);
        var grain = _clusterClient.GetGrain<IUserTaskGrain>(id);
        return await grain.GetUserTaskCompleteAsync(userTask);
    }
}