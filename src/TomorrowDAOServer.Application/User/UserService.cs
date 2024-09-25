using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Dto;
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

    public UserService(IUserProvider userProvider, IOptionsMonitor<UserOptions> userOptions,
        IUserVisitProvider userVisitProvider, IUserVisitSummaryProvider userVisitSummaryProvider, 
        IUserPointsRecordProvider userPointsRecordProvider, 
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, IReferralInviteProvider referralInviteProvider)
    {
        _userProvider = userProvider;
        _userOptions = userOptions;
        _userVisitProvider = userVisitProvider;
        _userVisitSummaryProvider = userVisitSummaryProvider;
        _userPointsRecordProvider = userPointsRecordProvider;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _referralInviteProvider = referralInviteProvider;
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
        if (!Enum.TryParse<UserTask>(input.UserTask, out var userTask))
        {
            throw new UserFriendlyException("Invalid UserTask.");
        }
        
        if (!Enum.TryParse<UserTaskDetail>(input.UserTaskDetail, out var userTaskDetail))
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
        var taskDictionary = dailyTaskList.ToDictionary(task => task.UserTaskDetail.ToString(), task => task);
        var completeCount = await _referralInviteProvider.GetInviteCountAsync(chainId, address);
        var taskDetails = userTask == UserTask.Daily
            ? TaskPointsHelper.InitDailyTaskDetailList()
            : TaskPointsHelper.InitExploreTaskDetailList(completeCount);
       
        
        foreach (var taskDetail in taskDetails)
        {
            if (!taskDictionary.TryGetValue(taskDetail.UserTaskDetail, out var task))
            {
                continue;
            }

            taskDetail.Points = task.Points;
            taskDetail.Complete = true;
        }

        return taskDetails;
    }
}