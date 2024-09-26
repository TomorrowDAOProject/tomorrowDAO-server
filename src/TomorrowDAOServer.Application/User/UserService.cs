using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver.Linq;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Referral.Provider;
using TomorrowDAOServer.User.Dtos;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Users;

namespace TomorrowDAOServer.User;

public class UserService : TomorrowDAOServerAppService, IUserService
{
    private readonly IUserProvider _userProvider;
    private readonly IOptionsMonitor<UserOptions> _userOptions;
    private readonly IUserVisitProvider _userVisitProvider;
    private readonly IUserVisitSummaryProvider _userVisitSummaryProvider;
    private readonly IUserPointsRecordProvider _userPointsRecordProvider;
    private readonly IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;
    private readonly IReferralInviteProvider _referralInviteProvider;
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;

    public UserService(IUserProvider userProvider, IOptionsMonitor<UserOptions> userOptions,
        IUserVisitProvider userVisitProvider, IUserVisitSummaryProvider userVisitSummaryProvider, 
        IUserPointsRecordProvider userPointsRecordProvider, 
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, IReferralInviteProvider referralInviteProvider, 
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider)
    {
        _userProvider = userProvider;
        _userOptions = userOptions;
        _userVisitProvider = userVisitProvider;
        _userVisitSummaryProvider = userVisitSummaryProvider;
        _userPointsRecordProvider = userPointsRecordProvider;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _referralInviteProvider = referralInviteProvider;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
    }

    public async Task<UserSourceReportResultDto> UserSourceReportAsync(string chainId, string source)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var userSourceList = _userOptions.CurrentValue.UserSourceList;
        if (!userSourceList.Contains(source, StringComparer.OrdinalIgnoreCase))
        {
            return new UserSourceReportResultDto
            {
                Success = false, Reason = "Invalid source."
            };
        }
        var matchedSource = userSourceList.FirstOrDefault(s => 
            string.Equals(s, source, StringComparison.OrdinalIgnoreCase));
        var now = TimeHelper.GetTimeStampInMilliseconds();
        var visitType = UserVisitType.Votigram;
        await _userVisitProvider.AddOrUpdateAsync(new UserVisitIndex
        {
            Id = GuidHelper.GenerateId(address, chainId, visitType.ToString(), matchedSource, now.ToString()),
            ChainId = chainId,
            Address = address,
            UserVisitType = visitType,
            Source = matchedSource!,
            VisitTime = now
        });
        var summaryId = GuidHelper.GenerateId(address, chainId, visitType.ToString(), matchedSource);
        var visitSummaryIndex = await _userVisitSummaryProvider.GetByIdAsync(summaryId);
        if (visitSummaryIndex == null)
        {
            visitSummaryIndex = new UserVisitSummaryIndex
            {
                Id = summaryId,
                ChainId = chainId,
                Address = address,
                UserVisitType = visitType,
                Source = matchedSource!,
                CreateTime = now,
                ModificationTime = now
            };
        }
        else
        {
            visitSummaryIndex.ModificationTime = now;
        }
        await _userVisitSummaryProvider.AddOrUpdateAsync(visitSummaryIndex);
        
        return new UserSourceReportResultDto
        {
            Success = true
        };
    }

    public async Task<bool> CompleteTaskAsync(CompleteTaskInput input)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        var (userTask, userTaskDetail) = CheckUserTask(input);
        var completeTime = DateTime.UtcNow;
        var success = await _userPointsRecordProvider.UpdateUserTaskCompleteTimeAsync(input.ChainId, address, userTask, userTaskDetail, completeTime);
        if (!success)
        {
            throw new UserFriendlyException("Task already completed.");
        }

        await _rankingAppPointsRedisProvider.IncrementTaskPointsAsync(address, userTaskDetail);
        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(input.ChainId, address, userTaskDetail, completeTime);
        return true;
    }

    public async Task<VoteHistoryPagedResultDto<MyPointsDto>> GetMyPointsAsync(GetMyPointsInput input)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        var totalPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(address);
        var (count, list) = await _userPointsRecordProvider.GetPointsListAsync(input, address);
        var data = new List<MyPointsDto>();
        foreach (var pointsRecord in list)
        {
            var (title, desc) = GetTitleAndDesc(pointsRecord);
            data.Add(new MyPointsDto
            {
                Points = pointsRecord.Points,
                Title = title,
                Description = desc
            });
        }

        return new VoteHistoryPagedResultDto<MyPointsDto>(count, data, totalPoints);
    }

    public async Task<TaskListDto> GetTaskListAsync(string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var dailyTaskList = await _userPointsRecordProvider.GetByAddressAndUserTaskAsync(chainId, address, UserTask.Daily);
        var exploreTaskList = await _userPointsRecordProvider.GetByAddressAndUserTaskAsync(chainId, address, UserTask.Explore);
        var dailyTaskInfoList = await GenerateTaskInfoDetails(chainId, address, dailyTaskList, UserTask.Daily);
        var exploreTaskInfoList = await GenerateTaskInfoDetails(chainId, address, exploreTaskList, UserTask.Explore);
        return new TaskListDto
        {
            TaskList = new List<TaskInfo>
            {
                new()
                {
                    TotalCount = dailyTaskInfoList.Count, Data = dailyTaskInfoList,
                    UserTask = UserTask.Daily.ToString()
                },
                new()
                {
                    TotalCount = exploreTaskInfoList.Count, Data = exploreTaskInfoList,
                    UserTask = UserTask.Explore.ToString()
                }
            }
        };
    }

    private Tuple<UserTask, UserTaskDetail> CheckUserTask(CompleteTaskInput input)
    {
        if (!Enum.TryParse<UserTask>(input.UserTask, out var userTask) || UserTask.None == userTask)
        {
            throw new UserFriendlyException("Invalid UserTask.");
        }
        
        if (!Enum.TryParse<UserTaskDetail>(input.UserTaskDetail, out var userTaskDetail) || UserTaskDetail.None == userTaskDetail)
        {
            throw new UserFriendlyException("Invalid UserTaskDetail.");
        }

        if (userTask != TaskPointsHelper.GetUserTaskFromUserTaskDetail(userTaskDetail))
        {
            throw new UserFriendlyException("UserTaskDetail and UserTask not match.");
        }

        if (!TaskPointsHelper.FrontEndTaskDetails.Contains(userTaskDetail))
        {
            throw new UserFriendlyException("Can not complete UserTaskDetail " + userTaskDetail);
        }

        return new Tuple<UserTask, UserTaskDetail>(userTask, userTaskDetail);
    }

    private Tuple<string, string> GetTitleAndDesc(UserPointsIndex index)
    {
        var information = index.Information;
        var pointsType = index.PointsType;
        var id = index.Id;
        var chainId = index.ChainId;
        switch (pointsType)
        {
            case PointsType.Like:
                return new Tuple<string, string>("Like", string.Empty);
            case PointsType.Vote:
                var alias = information.GetValueOrDefault(CommonConstant.Alias, string.Empty);
                var proposalTitle = information.GetValueOrDefault(CommonConstant.ProposalTitle, string.Empty);
                return new Tuple<string, string>("Voted for: " + alias, proposalTitle);
            case PointsType.InviteVote:
                var invitee = GetIndexString(id,2, CommonConstant.Middleline);
                return new Tuple<string, string>("Invite friends", "Invitee : ELF_" + invitee + "_" + chainId);
            case PointsType.BeInviteVote:
                var inviter = GetIndexString(id,1, CommonConstant.Middleline);
                return new Tuple<string, string>("Accept Invitation", "Inviter : ELF_" + inviter + "_" + chainId);
            case PointsType.TopInviter:
                var startTime = TimeHelper.ConvertStrTimeToDate(GetIndexString(id, 3, CommonConstant.Middleline));
                var endTime = TimeHelper.ConvertStrTimeToDate(GetIndexString(id, 4, CommonConstant.Middleline));
                return new Tuple<string, string>("Top 10 Inviters", "Cycle: " + startTime + "-" + endTime);
            default:
                return new Tuple<string, string>(pointsType.ToString(), string.Empty);
        }
    }

    private string GetIndexString(string str, int index, string splitSymbol)
    {
        try
        { 
            return str.Split(splitSymbol)[index];
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
    
    private async Task<List<TaskInfoDetail>> GenerateTaskInfoDetails(string chainId, string address, List<UserPointsIndex> dailyTaskList, UserTask userTask)
    {
        var taskDictionary = dailyTaskList
            .GroupBy(task => task.UserTaskDetail.ToString())
            .Select(g => g.OrderByDescending(task => task.PointsTime).First())
            .ToDictionary(task => task.UserTaskDetail.ToString(), task => task);
        var defaultProposalProposalId = await _rankingAppPointsRedisProvider.GetDefaultRankingProposalIdAsync(chainId);
        var latestDailyVote = dailyTaskList.Where(x => x.UserTaskDetail == UserTaskDetail.DailyVote)
            .MaxBy(x => x.PointsTime);
        var latestDailyVoteProposalId = latestDailyVote.Information.GetValueOrDefault(CommonConstant.ProposalId, string.Empty);
        if (defaultProposalProposalId != latestDailyVoteProposalId)
        {
            taskDictionary.Remove(UserTaskDetail.DailyVote.ToString());
        }
        var completeCount = await _referralInviteProvider.GetInviteCountAsync(chainId, address);
        var taskDetails = userTask == UserTask.Daily
            ? InitDailyTaskDetailList()
            : InitExploreTaskDetailList(completeCount);
        
        foreach (var taskDetail in taskDetails.Where(taskDetail => taskDictionary.TryGetValue(taskDetail.UserTaskDetail, out _)))
        {
            taskDetail.Complete = true;
        }

        return taskDetails;
    }
    
    private List<TaskInfoDetail> InitDailyTaskDetailList()
    {
        return new List<TaskInfoDetail>
        {
            new() { UserTaskDetail = UserTaskDetail.DailyVote.ToString(), Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.Vote)},
            new() { UserTaskDetail = UserTaskDetail.DailyFirstInvite.ToString(), Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.DailyFirstInvite) },
            new() { UserTaskDetail = UserTaskDetail.DailyViewAsset.ToString(), Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.DailyViewAsset) }
        };
    }
    
    private List<TaskInfoDetail> InitExploreTaskDetailList(long completeCount)
    {
        return new List<TaskInfoDetail>
        {
            new() { UserTaskDetail = UserTaskDetail.ExploreJoinTgChannel.ToString(), Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreJoinTgChannel) },
            new() { UserTaskDetail = UserTaskDetail.ExploreFollowX.ToString(), Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreFollowX) },
            new() { UserTaskDetail = UserTaskDetail.ExploreJoinDiscord.ToString(), Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreJoinDiscord) },
            new() { UserTaskDetail = UserTaskDetail.ExploreCumulateFiveInvite.ToString(), Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreCumulateFiveInvite), CompleteCount = completeCount, TaskCount = 5 },
            new() { UserTaskDetail = UserTaskDetail.ExploreCumulateTenInvite.ToString(), Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreCumulateTenInvite), CompleteCount = completeCount, TaskCount = 10 },
            new() { UserTaskDetail = UserTaskDetail.ExploreCumulateTwentyInvite.ToString(), Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreCumulateTwentyInvite), CompleteCount = completeCount, TaskCount = 20 }
        };
    }
}