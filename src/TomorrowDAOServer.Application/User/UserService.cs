using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aetherlink.PriceServer.Common;
using Microsoft.Extensions.Logging;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Dto;
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
    private readonly ITelegramUserInfoProvider _telegramUserInfoProvider;
    private readonly ILogger<UserService> _logger;
    private readonly IProposalProvider _proposalProvider;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private readonly IRankingAppProvider _rankingAppProvider;


    public UserService(IUserProvider userProvider, IOptionsMonitor<UserOptions> userOptions,
        IUserVisitProvider userVisitProvider, IUserVisitSummaryProvider userVisitSummaryProvider,
        IUserPointsRecordProvider userPointsRecordProvider,
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, IReferralInviteProvider referralInviteProvider,
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider, ITelegramAppsProvider telegramAppsProvider, ILogger<UserService> logger, 
        ITelegramUserInfoProvider telegramUserInfoProvider, IProposalProvider proposalProvider, IOptionsMonitor<RankingOptions> rankingOptions,
        IRankingAppProvider rankingAppProvider)
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
        _logger = logger;
        _telegramUserInfoProvider = telegramUserInfoProvider;
        _proposalProvider = proposalProvider;
        _rankingOptions = rankingOptions;
        _rankingAppProvider = rankingAppProvider;
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
                        t.Information.ContainsKey(CommonConstant.Alias) && t.Information[CommonConstant.Alias] != null &&
                        !t.Information[CommonConstant.Alias].IsNullOrWhiteSpace()).Select(t =>
                t.Information.GetValueOrDefault(CommonConstant.Alias, string.Empty)).ToList();

        if (aliases.IsNullOrEmpty())
        {
            return new Dictionary<string, string>();
        }
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

    public async Task<long> ViewAdAsync(ViewAdInput input)
    {
        var checkKey = _userOptions.CurrentValue.CheckKey;
        var timeStamp = input.TimeStamp;
        var signature = input.Signature;
        var chainId = input.ChainId;
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var hashString = Sha256HashHelper.ComputeSha256Hash(IdGeneratorHelper.GenerateId(checkKey, timeStamp));
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
        var adTime = DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).UtcDateTime;
        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, address, UserTaskDetail.DailyViewAds, adTime, information);
        return await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(address);
    }

    public async Task<bool> SaveTgInfoAsync(SaveTgInfoInput input)
    {
        var chainId = input.ChainId;
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        await _telegramUserInfoProvider.AddOrUpdateAsync(new TelegramUserInfoIndex
        {
            Id = GuidHelper.GenerateGrainId(chainId, address), ChainId = chainId, Address = address, 
            Icon = input.Icon, FirstName = input.FirstName, LastName = input.LastName, UserName = input.UserName,
            UpdateTime = DateTime.UtcNow
        });
        return true;
    }

    public async Task GenerateDailyCreatePollPointsAsync(string chainId, List<IndexerProposal> proposalList)
    {
        foreach (var proposal in proposalList)
        {
            var completeTime = proposal.DeployTime;
            var address = proposal.Proposer;
            var success = await _userPointsRecordProvider.UpdateUserTaskCompleteTimeAsync(chainId, address, UserTask.Daily,
                UserTaskDetail.DailyCreatePoll, completeTime);
            if (!success)
            {
                continue;
            }
            await _rankingAppPointsRedisProvider.IncrementTaskPointsAsync(address, UserTaskDetail.DailyCreatePoll);
            var information = InformationHelper.GetDailyCreatePollInformation(proposal);
            await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, address, UserTaskDetail.DailyCreatePoll, completeTime, information);
        }
    }
    
    [ExceptionHandler(typeof(Exception),  TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultThrowMethodName,Message = "Get login point status fail.", LogTargets = new []{"input"})]
    public virtual async Task<LoginPointsStatusDto> GetLoginPointsStatusAsync(GetLoginPointsStatusInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var userExtraDto = userGrainDto.GetUserExtraDto();
        userExtraDto ??= new UserExtraDto();

        if (TimeHelper.IsYesterday(userExtraDto.LastModifiedTime))
        {
            userExtraDto.ConsecutiveLoginDays += 1;
            userExtraDto.DailyLoginPointsStatus = false;
            userExtraDto.LastModifiedTime = DateTime.UtcNow;
        }
        else if (!TimeHelper.IsToday(userExtraDto.LastModifiedTime))
        {
            userExtraDto.ConsecutiveLoginDays = 1;
            userExtraDto.DailyLoginPointsStatus = false;
            userExtraDto.LastModifiedTime = DateTime.UtcNow;
        }
        
        userGrainDto.SetUserExtraDto(userExtraDto);
        await _userProvider.UpdateUserAsync(userGrainDto);

        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var totalPoints = await GetTotalPoints(address, userGrainDto.UserId.ToString());
        
        return new LoginPointsStatusDto
        {
            ConsecutiveLoginDays = userExtraDto.ConsecutiveLoginDays,
            DailyLoginPointsStatus = userExtraDto.DailyLoginPointsStatus,
            UserTotalPoints = totalPoints
        };
    }

    [ExceptionHandler(typeof(Exception),  TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultThrowMethodName,Message = "Collect login point fail.", LogTargets = new []{"input"})]
    public virtual async Task<LoginPointsStatusDto> CollectLoginPointsAsync(CollectLoginPointsInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var userExtraDto = userGrainDto.GetUserExtraDto();
        if (userExtraDto == null)
        {
            throw new UserFriendlyException("Extra info is invalid");
        }

        if (userExtraDto.DailyLoginPointsStatus)
        {
            throw new UserFriendlyException("Already claimed the rewards points.");
        }

        var viewAd = false;
        if (!input.Signature.IsNullOrWhiteSpace() && !input.TimeStamp.IsNullOrWhiteSpace())
        {
            var checkKey = _userOptions.CurrentValue.CheckKey;
            var timeStamp = input.TimeStamp;
            var signature = input.Signature;
            var hashString = Sha256HashHelper.ComputeSha256Hash(IdGeneratorHelper.GenerateId(checkKey, timeStamp));
            if (hashString == signature)
            {
                viewAd = true;
            }
        }

        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        if (!address.IsNullOrWhiteSpace())
        {
            await _rankingAppPointsRedisProvider.IncrementLoginPointsAsync(address, viewAd,
                userExtraDto.ConsecutiveLoginDays);
        }
        else
        {
            await _rankingAppPointsRedisProvider.IncrementLoginPointsByUserIdAsync(userGrainDto.UserId.ToString(),
                viewAd, userExtraDto.ConsecutiveLoginDays);
        }
        
        var lastModifiedTime = DateTime.UtcNow;
        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(input.ChainId, address ?? string.Empty,
            UserTaskDetail.DailyLogin, PointsType.DailyLogin, lastModifiedTime,
            new Dictionary<string, string>()
            {
                { "consecutiveLoginDays", userExtraDto.ConsecutiveLoginDays.ToString() },
                { "viewAd", viewAd.ToString() }
            }, userGrainDto.UserId.ToString());

        userExtraDto.DailyLoginPointsStatus = true;
        userExtraDto.LastModifiedTime = lastModifiedTime;
        await _userProvider.UpdateUserAsync(userGrainDto);
        
        var totalPoints = await GetTotalPoints(address, userGrainDto.UserId.ToString());

        return new LoginPointsStatusDto
        {
            ConsecutiveLoginDays = userExtraDto.ConsecutiveLoginDays,
            DailyLoginPointsStatus = userExtraDto.DailyLoginPointsStatus,
            UserTotalPoints = totalPoints
        };
    }

    [ExceptionHandler(typeof(Exception),  TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultThrowMethodName,Message = "Get home page fail.", LogTargets = new []{"input"})]
    public async Task<HomePageResultDto> GetHomePageAsync(GetHomePageInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        var totalPoints = await GetTotalPoints(address, userId);
        
        //Weekly Top Voted Apps
        var weeklyTopVotedApps = await GetWeeklyTopVotedApps(input);
        
        //Discover Hidden Game
        RankingAppDetailDto discoverHiddenGame = await GetDiscoverHiddenGame(input.ChainId, address, userId);

        return new HomePageResultDto
        {
            UserTotalPoints = totalPoints,
            WeeklyTopVotedApps = weeklyTopVotedApps,
            DiscoverHiddenGems = discoverHiddenGame
        };
    }

    private async Task<List<RankingAppDetailDto>> GetWeeklyTopVotedApps(GetHomePageInput input)
    {
        var topRankingAddress = _rankingOptions.CurrentValue.TopRankingAddress;
        var proposal = await _proposalProvider.GetTopProposalAsync(topRankingAddress, true);
        if (proposal == null)
        {
            return new List<RankingAppDetailDto>();
        }

        var proposalId = proposal.ProposalId;
        var rankingAppList =
            await _rankingAppProvider.GetByProposalIdAsync(input.ChainId, proposalId);
        if (rankingAppList.IsNullOrEmpty())
        {
            return new List<RankingAppDetailDto>();
        }
        
        var rankingList = ObjectMapper.Map<List<RankingAppIndex>, List<RankingAppDetailDto>>(rankingAppList);

        var aliasList = rankingList.Select(t => t.Alias).Distinct().ToList();
        var appPointsList = await _rankingAppPointsRedisProvider.GetAllAppPointsAsync(input.ChainId, proposalId, aliasList);
        var appVoteAmountDic = appPointsList
            .Where(x => x.PointsType == PointsType.Vote)
            .ToDictionary(x => x.Alias, x => _rankingAppPointsCalcProvider.CalculateVotesFromPoints(x.Points));
        var appPointsDic = RankingAppPointsDto.ConvertToBaseList(appPointsList)
            .ToDictionary(x => x.Alias, x => x.Points);
        var totalVoteAmount = appVoteAmountDic.Values.Sum();
        var totalPoints = appPointsList.Sum(x => x.Points);
        var votePercentFactor = DoubleHelper.GetFactor(totalVoteAmount);
        var pointsPercentFactor = DoubleHelper.GetFactor(totalPoints);
        foreach (var rankingAppDetailDto in rankingList)
        {
            var icon = rankingAppDetailDto.Icon;
            var needPrefix = !string.IsNullOrEmpty(icon) && icon.StartsWith("/");
            if (needPrefix)
            {
                rankingAppDetailDto.Icon = CommonConstant.FindminiUrlPrefix + icon;
            }

            var alias = rankingAppDetailDto.Alias;
            rankingAppDetailDto.PointsAmount = appPointsDic.GetValueOrDefault(alias, 0);
            rankingAppDetailDto.VoteAmount = appVoteAmountDic.GetValueOrDefault(alias, 0);
            rankingAppDetailDto.VotePercent = appVoteAmountDic.GetValueOrDefault(alias, 0) * votePercentFactor;
            rankingAppDetailDto.PointsPercent = rankingAppDetailDto.PointsAmount * pointsPercentFactor;
        }
        return rankingList
            .Where(r => r.PointsAmount > 0) 
            .OrderByDescending(r => r.PointsAmount) 
            .ThenBy(r => aliasList.IndexOf(r.Alias))
            .Take(6)
            .ToList();
    }
    
    private async Task<RankingAppDetailDto> GetDiscoverHiddenGame(string inputChainId, string address, string userId)
    {
        //TODO GetDiscoverHiddenGame
        return new RankingAppDetailDto();
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
                var alias = information.GetValueOrDefault(CommonConstant.Alias, string.Empty) ?? string.Empty;
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
            case PointsType.DailyViewAds:
                var adPlatform = information.GetValueOrDefault(CommonConstant.AdPlatform, string.Empty);
                return new Tuple<string, string>("Watch Ads", adPlatform);
            case PointsType.DailyCreatePoll:
                return new Tuple<string, string>("Task", "Create your poll");
            case PointsType.ExploreJoinVotigram:
                return new Tuple<string, string>("Task", "Join Votigram channel");
            case PointsType.ExploreFollowVotigramX:
                return new Tuple<string, string>("Task", "Follow Votigram on X");
            case PointsType.ExploreForwardVotigramX:
                return new Tuple<string, string>("Task", "RT Votigram Post");
            default:
                return new Tuple<string, string>(pointsType.ToString(), string.Empty);
        }
    }

    private async Task<List<TaskInfoDetail>> GenerateTaskInfoDetails(string chainId, string address,
        List<UserPointsIndex> taskList, UserTask userTask)
    {
        var taskDictionary = taskList
            .GroupBy(task => task.UserTaskDetail.ToString())
            .Select(g => g.OrderByDescending(task => task.PointsTime).First())
            .ToDictionary(task => task.UserTaskDetail.ToString(), task => task);
        var taskDetails = userTask == UserTask.Daily
            ? InitDailyTaskDetailList(await _userPointsRecordProvider.GetDailyViewAdCountAsync(chainId, address))
            : InitExploreTaskDetailList(await _referralInviteProvider.GetInviteCountAsync(chainId, address));

        foreach (var taskDetail in taskDetails.Where(taskDetail =>
                     taskDictionary.TryGetValue(taskDetail.UserTaskDetail, out _)))
        {
            if (UserTaskDetail.DailyViewAds.ToString() == taskDetail.UserTaskDetail)
            {
                taskDetail.Complete = taskDetail.CompleteCount >= taskDetail.TaskCount;
            }
            else
            {
                taskDetail.Complete = true;
            }
        }

        return taskDetails;
    }

    private List<TaskInfoDetail> InitDailyTaskDetailList(long adCount)
    {
        return new List<TaskInfoDetail>
        {
            new()
            {
                UserTaskDetail = UserTaskDetail.DailyViewAds.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.DailyViewAds),
                CompleteCount = adCount, TaskCount = 20
            },
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
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.DailyCreatePoll.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.DailyCreatePoll),
            },
        };
    }

    private List<TaskInfoDetail> InitExploreTaskDetailList(long completeCount)
    {
        return new List<TaskInfoDetail>
        {
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreJoinVotigram.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreJoinVotigram)
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreFollowVotigramX.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreFollowVotigramX)
            },
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreForwardVotigramX.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreForwardVotigramX)
            },
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
    
    private async Task<long> GetTotalPoints(string address, string userId)
    {
        var totalPoints = 0L;
        if (!address.IsNullOrWhiteSpace())
        {
            totalPoints += await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(address);
        }

        if (!userId.IsNullOrWhiteSpace())
        {
            totalPoints += await _rankingAppPointsRedisProvider.GetUserAllPointsByIdAsync(userId);
        }
        
        return totalPoints;
    }
}