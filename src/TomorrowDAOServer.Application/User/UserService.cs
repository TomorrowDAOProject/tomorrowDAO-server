using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using Aetherlink.PriceServer.Common;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Referral.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
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
    private readonly ITelegramAppsProvider _telegramAppsProvider;

    public UserService(IUserProvider userProvider, IOptionsMonitor<UserOptions> userOptions,
        IUserVisitProvider userVisitProvider, IUserVisitSummaryProvider userVisitSummaryProvider,
        IUserPointsRecordProvider userPointsRecordProvider,
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, IReferralInviteProvider referralInviteProvider,
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider, ITelegramAppsProvider telegramAppsProvider)
    {
        _userProvider = userProvider;
        _userOptions = userOptions;
        _userVisitProvider = userVisitProvider;
        _userVisitSummaryProvider = userVisitSummaryProvider;
        _userPointsRecordProvider = userPointsRecordProvider;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _referralInviteProvider = referralInviteProvider;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
        _telegramAppsProvider = telegramAppsProvider;
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
        var success = await _userPointsRecordProvider.UpdateUserTaskCompleteTimeAsync(input.ChainId, address, userTask,
            userTaskDetail, completeTime);
        if (!success)
        {
            throw new UserFriendlyException("Task already completed.");
        }

        await _rankingAppPointsRedisProvider.IncrementTaskPointsAsync(address, userTaskDetail);
        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(input.ChainId, address, userTaskDetail,
            completeTime);
        return true;
    }

    public async Task<VoteHistoryPagedResultDto<MyPointsDto>> GetMyPointsAsync(GetMyPointsInput input)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        var totalPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(address);
        var (count, list) = await _userPointsRecordProvider.GetPointsListAsync(input, address);
        var appNames = await GetAppNameAsync(list);
        var data = new List<MyPointsDto>();
        foreach (var pointsRecord in list)
        {
            var (title, desc) = GetTitleAndDesc(pointsRecord, appNames);
            data.Add(new MyPointsDto
            {
                Id = pointsRecord.Id, Points = pointsRecord.Points,
                Title = title, Description = desc,
                PointsType = pointsRecord.PointsType.ToString(),
                PointsTime = pointsRecord.PointsTime.ToUtcMilliSeconds()
            });
        }

        return new VoteHistoryPagedResultDto<MyPointsDto>(count, data, totalPoints);
    }

    private async Task<Dictionary<string, string>> GetAppNameAsync(List<UserPointsIndex> userPointsIndices)
    {
        if (userPointsIndices.IsNullOrEmpty())
        {
            return new Dictionary<string, string>();
        }

        var aliases =  userPointsIndices
            .Where(t => t.PointsType == PointsType.Vote && t.Information != null &&
                        t.Information.ContainsKey(CommonConstant.Alias) &&
                        !t.Information[CommonConstant.Alias].IsNullOrWhiteSpace()).Select(t =>
                t.Information.GetValueOrDefault(CommonConstant.Alias, string.Empty)).ToList();

        var telegramApps = await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
        {
            Aliases = aliases
        });
        if (telegramApps.Item2.IsNullOrEmpty())
        {
            return new Dictionary<string, string>();
        }

        var dictionary = new Dictionary<string, string>();
        foreach (var telegramAppIndex in telegramApps.Item2)
        {
            dictionary[telegramAppIndex.Alias] = telegramAppIndex.Title;
        }

        return dictionary;
    }

    public async Task<TaskListDto> GetTaskListAsync(string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var dailyTaskList =
            await _userPointsRecordProvider.GetByAddressAndUserTaskAsync(chainId, address, UserTask.Daily);
        var exploreTaskList =
            await _userPointsRecordProvider.GetByAddressAndUserTaskAsync(chainId, address, UserTask.Explore);
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

    public async Task<bool> ViewAdAsync(ViewAdInput input)
    {
        var checkKey = _userOptions.CurrentValue.CheckKey;
        var timeStamp = input.TimeStamp;
        var signature = input.Signature;
        var chainId = input.ChainId;
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var hashString = HashHelper.ComputeFrom(IdGeneratorHelper.GenerateId(checkKey, timeStamp)).ToHex();
        if (hashString != signature)
        {
            throw new UserFriendlyException("Invalid signature.");
        }

        var timeCheck = await _userPointsRecordProvider.UpdateUserViewAdTimeStampAsync(chainId, address, timeStamp);
        if (!timeCheck)
        {
            throw new UserFriendlyException("Invalid timeStamp.");
        }

        var information = InformationHelper.GetViewAdInformation(AdPlatform.Adsgram.ToString(), timeStamp);
        await _rankingAppPointsRedisProvider.IncrementViewAdPointsAsync(address);
        await _userPointsRecordProvider.GeneratePointsRecordAsync(chainId, address, PointsType.ViewAd, timeStamp,
            information);
        return true;
    }

    public Task<string> GetAdHashAsync(long timeStamp)
    { 
        var checkKey = _userOptions.CurrentValue.CheckKey;
        return Task.FromResult(HashHelper.ComputeFrom(IdGeneratorHelper.GenerateId(checkKey, timeStamp)).ToHex());
    }

    private Tuple<UserTask, UserTaskDetail> CheckUserTask(CompleteTaskInput input)
    {
        if (!Enum.TryParse<UserTask>(input.UserTask, out var userTask) || UserTask.None == userTask)
        {
            throw new UserFriendlyException("Invalid UserTask.");
        }

        if (!Enum.TryParse<UserTaskDetail>(input.UserTaskDetail, out var userTaskDetail) ||
            UserTaskDetail.None == userTaskDetail)
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

    private Tuple<string, string> GetTitleAndDesc(UserPointsIndex index, Dictionary<string, string> appNames)
    {
        var information = index.Information;
        var pointsType = index.PointsType;
        var chainId = index.ChainId;
        switch (pointsType)
        {
            case PointsType.Vote:
                var alias = information.GetValueOrDefault(CommonConstant.Alias, string.Empty);
                alias = appNames.GetValueOrDefault(alias, alias);
                var proposalTitle = information.GetValueOrDefault(CommonConstant.ProposalTitle, string.Empty);
                return new Tuple<string, string>("Voted for: " + alias, proposalTitle);
            case PointsType.InviteVote:
                var invitee = information.GetValueOrDefault(CommonConstant.Invitee, string.Empty);
                return new Tuple<string, string>("Invite friends", "Invitee : ELF_" + invitee + "_" + chainId);
            case PointsType.BeInviteVote:
                var inviter = information.GetValueOrDefault(CommonConstant.Inviter, string.Empty);
                return new Tuple<string, string>("Accept Invitation", "Inviter : ELF_" + inviter + "_" + chainId);
            case PointsType.TopInviter:
                var startTime = information.GetValueOrDefault(CommonConstant.CycleStartTime, string.Empty);
                var endTime = information.GetValueOrDefault(CommonConstant.CycleEndTime, string.Empty);
                return new Tuple<string, string>("Top 10 Inviters", startTime + "-" + endTime);
            case PointsType.DailyViewAsset:
                return new Tuple<string, string>("Task", "View your assets");
            case PointsType.DailyFirstInvite:
                return new Tuple<string, string>("Task", "Invite 1 friend");
            case PointsType.ExploreJoinTgChannel:
                return new Tuple<string, string>("Task", "Join channel");
            case PointsType.ExploreFollowX:
                return new Tuple<string, string>("Task", "Follow us on X");
            case PointsType.ExploreJoinDiscord:
                return new Tuple<string, string>("Task", "Join Discord");
            case PointsType.ExploreForwardX:
                return new Tuple<string, string>("Task", "RT Post");
            case PointsType.ExploreCumulateFiveInvite:
                return new Tuple<string, string>("Task", "Invite 5 friends");
            case PointsType.ExploreCumulateTenInvite:
                return new Tuple<string, string>("Task", "Invite 10 friends");
            case PointsType.ExploreCumulateTwentyInvite:
                return new Tuple<string, string>("Task", "Invite 20 friends");
            case PointsType.ViewAd:
                var adPlatform = information.GetValueOrDefault(CommonConstant.AdPlatform, string.Empty);
                return new Tuple<string, string>("Click Ads", adPlatform);
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

    private async Task<List<TaskInfoDetail>> GenerateTaskInfoDetails(string chainId, string address,
        List<UserPointsIndex> dailyTaskList, UserTask userTask)
    {
        var taskDictionary = dailyTaskList
            .GroupBy(task => task.UserTaskDetail.ToString())
            .Select(g => g.OrderByDescending(task => task.PointsTime).First())
            .ToDictionary(task => task.UserTaskDetail.ToString(), task => task);
        // var latestDailyVote = dailyTaskList.Where(x => x.UserTaskDetail == UserTaskDetail.DailyVote)
        //     .MaxBy(x => x.PointsTime);
        // if (latestDailyVote != null)
        // {
        //     var defaultProposalProposalId =
        //         await _rankingAppPointsRedisProvider.GetDefaultRankingProposalIdAsync(chainId);
        //     var latestDailyVoteProposalId =
        //         latestDailyVote.Information.GetValueOrDefault(CommonConstant.ProposalId, string.Empty);
        //     if (defaultProposalProposalId != latestDailyVoteProposalId)
        //     {
        //         taskDictionary.Remove(UserTaskDetail.DailyVote.ToString());
        //     }
        // }

        var completeCount = await _referralInviteProvider.GetInviteCountAsync(chainId, address);
        var taskDetails = userTask == UserTask.Daily
            ? InitDailyTaskDetailList()
            : InitExploreTaskDetailList(completeCount);

        foreach (var taskDetail in taskDetails.Where(taskDetail =>
                     taskDictionary.TryGetValue(taskDetail.UserTaskDetail, out _)))
        {
            taskDetail.Complete = true;
        }

        return taskDetails;
    }

    private List<TaskInfoDetail> InitDailyTaskDetailList()
    {
        return new List<TaskInfoDetail>
        {
            new()
            {
                UserTaskDetail = UserTaskDetail.DailyVote.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.Vote, 1)
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.DailyFirstInvite.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.DailyFirstInvite)
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.DailyViewAsset.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.DailyViewAsset)
            }
        };
    }

    private List<TaskInfoDetail> InitExploreTaskDetailList(long completeCount)
    {
        return new List<TaskInfoDetail>
        {
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreJoinTgChannel.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreJoinTgChannel)
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreFollowX.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreFollowX)
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreJoinDiscord.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreJoinDiscord)
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreForwardX.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreForwardX)
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreCumulateFiveInvite.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType
                    .ExploreCumulateFiveInvite),
                CompleteCount = completeCount, TaskCount = 5
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreCumulateTenInvite.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(
                    PointsType.ExploreCumulateTenInvite),
                CompleteCount = completeCount, TaskCount = 10
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreCumulateTwentyInvite.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType
                    .ExploreCumulateTwentyInvite),
                CompleteCount = completeCount, TaskCount = 20
            }
        };
    }
    
    
}