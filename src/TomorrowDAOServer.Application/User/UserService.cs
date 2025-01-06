using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Aetherlink.PriceServer.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Discover.Provider;
using TomorrowDAOServer.Discussion.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.MQ;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Referral.Provider;
using TomorrowDAOServer.Schrodinger.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User.Dtos;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.ObjectMapping;
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
    private readonly ISchrodingerApiProvider _schrodingerApiProvider;
    private readonly IOptionsMonitor<SchrodingerOptions> _schrodingerOptions;
    private readonly IProposalProvider _proposalProvider;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly IDiscoverChoiceProvider _discoverChoiceProvider;
    private readonly IRankingAppPointsProvider _rankingAppPointsProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IMessagePublisherService _messagePublisherService;
    private readonly IDiscussionProvider _discussionProvider;

    public UserService(IUserProvider userProvider, IOptionsMonitor<UserOptions> userOptions,
        IUserVisitProvider userVisitProvider, IUserVisitSummaryProvider userVisitSummaryProvider,
        IUserPointsRecordProvider userPointsRecordProvider,
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, IReferralInviteProvider referralInviteProvider,
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider, ITelegramAppsProvider telegramAppsProvider, ILogger<UserService> logger, 
        ITelegramUserInfoProvider telegramUserInfoProvider, ISchrodingerApiProvider schrodingerApiProvider, 
        IOptionsMonitor<SchrodingerOptions> schrodingerOptions, IProposalProvider proposalProvider, IOptionsMonitor<RankingOptions> rankingOptions,
        IRankingAppProvider rankingAppProvider, IDiscoverChoiceProvider discoverChoiceProvider, IRankingAppPointsProvider rankingAppPointsProvider,
        IObjectMapper objectMapper, IMessagePublisherService messagePublisherService, IDiscussionProvider discussionProvider)
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
        _discoverChoiceProvider = discoverChoiceProvider;
        _rankingAppPointsProvider = rankingAppPointsProvider;
        _objectMapper = objectMapper;
        _messagePublisherService = messagePublisherService;
        _discussionProvider = discussionProvider;
        _schrodingerApiProvider = schrodingerApiProvider;
        _schrodingerOptions = schrodingerOptions;
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
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        
        var (userTask, userTaskDetail) = CheckUserTask(input);
        var completeTime = DateTime.UtcNow;
        if (userTaskDetail == UserTaskDetail.ExploreSchrodinger)
        {
            if (!await CheckSchrodinger(input.ChainId, address))
            {
                return false;
            }
        }
        
        var success = await _userPointsRecordProvider.UpdateUserTaskCompleteTimeAsync(input.ChainId, userId, address, userTask,
            userTaskDetail, completeTime);
        if (!success)
        {
            throw new UserFriendlyException("Task already completed.");
        }
        
        await _rankingAppPointsRedisProvider.IncrementTaskPointsAsync(address.IsNullOrWhiteSpace() ? userId : address, userTaskDetail);
        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(input.ChainId, address, userTaskDetail,
            completeTime, null, userId);
        return true;
    }

    public async Task<VoteHistoryPagedResultDto<MyPointsDto>> GetMyPointsAsync(GetMyPointsInput input)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        var totalPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsByAddressAsync(address);
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
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(chainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        
        var dailyTaskList =
            await _userPointsRecordProvider.GetByAddressAndUserTaskAsync(chainId, userId, address, new List<UserTask>{UserTask.Daily});
        _logger.LogInformation("get daily task list : {list}, chainId: {chainId}, userId: {userId},address: {address}", JsonConvert.SerializeObject(dailyTaskList), chainId, userId, address);
        var exploreTaskList =
            await _userPointsRecordProvider.GetByAddressAndUserTaskAsync(chainId, userId, address, new List<UserTask>
                {
                    UserTask.Explore, UserTask.ExploreVotigram, UserTask.ExploreApps, UserTask.Referrals
                });
        var dailyTaskInfoList = await GenerateTaskInfoDetails(chainId, userId, address, dailyTaskList, UserTask.Daily);
        var exploreVotigramTaskInfoList = await GenerateTaskInfoDetails(chainId, userId, address, exploreTaskList, UserTask.ExploreVotigram);
        var exploreAppTaskInfoList = await GenerateTaskInfoDetails(chainId, userId, address, exploreTaskList, UserTask.ExploreApps);
        var referralsTaskInfoList = await GenerateTaskInfoDetails(chainId, userId, address, exploreTaskList, UserTask.Referrals);
        var schrodingerValid = _schrodingerOptions.CurrentValue.Valid;
        if (!schrodingerValid)
        {
            exploreAppTaskInfoList.RemoveAll(task => task.UserTaskDetail == UserTaskDetail.ExploreSchrodinger.ToString());
        }
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
                    TotalCount = exploreVotigramTaskInfoList.Count, Data = exploreVotigramTaskInfoList,
                    UserTask = UserTask.ExploreVotigram.ToString()
                },
                new() {
                    TotalCount = exploreAppTaskInfoList.Count, Data = exploreAppTaskInfoList,
                    UserTask = UserTask.ExploreApps.ToString()
                },
                new()
                {
                    TotalCount = referralsTaskInfoList.Count, Data = referralsTaskInfoList,
                    UserTask = UserTask.Referrals.ToString()
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
        //var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(chainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        
        var hashString = Sha256HashHelper.ComputeSha256Hash(IdGeneratorHelper.GenerateId(checkKey, timeStamp));
        if (hashString != signature)
        {
            throw new UserFriendlyException("Invalid signature.");
        }

        var timeCheck = await _userPointsRecordProvider.UpdateUserViewAdTimeStampAsync(chainId, userId, timeStamp);
        if (!timeCheck)
        {
            throw new UserFriendlyException("Invalid timeStamp.");
        }

        var information = InformationHelper.GetViewAdInformation(AdPlatform.Adsgram.ToString(), timeStamp);
        await _rankingAppPointsRedisProvider.IncrementViewAdPointsAsync(address.IsNullOrWhiteSpace() ? userId : address);
        var adTime = DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).UtcDateTime;
        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, address, UserTaskDetail.DailyViewAds, adTime, information, userId);
        return await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(userId, address);
    }

    public async Task<bool> SaveTgInfoAsync(SaveTgInfoInput input)
    {
        var chainId = input.ChainId;
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        await _telegramUserInfoProvider.AddOrUpdateAsync(new TelegramUserInfoIndex
        {
            Id = GuidHelper.GenerateGrainId(chainId, address), ChainId = chainId, Address = address, 
            Icon = input.Icon, FirstName = input.FirstName, LastName = input.LastName, UserName = input.UserName,
            TelegramId = input.TelegramId, UpdateTime = DateTime.UtcNow
        });
        return true;
    }

    public async Task GenerateDailyCreatePollPointsAsync(string chainId, List<IndexerProposal> proposalList)
    {
        foreach (var proposal in proposalList)
        {
            var completeTime = proposal.DeployTime;
            var address = proposal.Proposer;
            var success = await _userPointsRecordProvider.UpdateUserTaskCompleteTimeAsync(chainId, string.Empty,
                address, UserTask.Daily, UserTaskDetail.DailyCreatePoll, completeTime);
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
            if (userExtraDto.ConsecutiveLoginDays == 7)
            {
                userExtraDto.ConsecutiveLoginDays = 1;
                userExtraDto.DailyPointsClaimedStatus = new bool[7];
                userExtraDto.LastModifiedTime = DateTime.UtcNow;
            }
            else
            {
                userExtraDto.ConsecutiveLoginDays += 1;
                userExtraDto.LastModifiedTime = DateTime.UtcNow;
            }
        }
        else if (!TimeHelper.IsToday(userExtraDto.LastModifiedTime))
        {
            userExtraDto.ConsecutiveLoginDays = 1;
            userExtraDto.DailyPointsClaimedStatus = new bool[7];
            userExtraDto.LastModifiedTime = DateTime.UtcNow;
        }
        
        userGrainDto.SetUserExtraDto(userExtraDto);
        await _userProvider.UpdateUserAsync(userGrainDto);

        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var totalPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(userGrainDto.UserId.ToString(), address);
        
        return new LoginPointsStatusDto
        {
            ConsecutiveLoginDays = userExtraDto.ConsecutiveLoginDays,
            DailyLoginPointsStatus = userExtraDto.DailyPointsClaimedStatus[userExtraDto.ConsecutiveLoginDays - 1],
            DailyPointsClaimedStatus = userExtraDto.DailyPointsClaimedStatus,
            UserTotalPoints = totalPoints
        };
    }

    [ExceptionHandler(typeof(Exception),  TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultThrowMethodName, Message = "Collect login point fail.", LogTargets = new []{"input"})]
    public virtual async Task<LoginPointsStatusDto> CollectLoginPointsAsync(CollectLoginPointsInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var userExtraDto = userGrainDto.GetUserExtraDto();
        if (userExtraDto == null)
        {
            throw new UserFriendlyException("Extra info is invalid");
        }
        
        if (userExtraDto.DailyPointsClaimedStatus[userExtraDto.ConsecutiveLoginDays - 1])
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

        userExtraDto.DailyPointsClaimedStatus[userExtraDto.ConsecutiveLoginDays - 1] = true;
        userExtraDto.LastModifiedTime = lastModifiedTime;
        userGrainDto.SetUserExtraDto(userExtraDto);
        await _userProvider.UpdateUserAsync(userGrainDto);
        
        var totalPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(userGrainDto.UserId.ToString(), address);

        return new LoginPointsStatusDto
        {
            ConsecutiveLoginDays = userExtraDto.ConsecutiveLoginDays,
            DailyLoginPointsStatus = userExtraDto.DailyPointsClaimedStatus[userExtraDto.ConsecutiveLoginDays - 1],
            DailyPointsClaimedStatus = userExtraDto.DailyPointsClaimedStatus,
            UserTotalPoints = totalPoints
        };
    }

    [ExceptionHandler(typeof(Exception),  TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultThrowMethodName,Message = "Get home page fail.", LogTargets = new []{"input"})]
    public virtual async Task<HomePageResultDto> GetHomePageAsync(GetHomePageInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        var totalPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(userId, address);
        
        //Weekly Top Voted Apps
        var weeklyTopVotedApps = await GetWeeklyTopVotedApps(input);
        
        //Discover Hidden Game
        var discoverHiddenGame = await GetDiscoverHiddenGame(input.ChainId, address, userId);

        return new HomePageResultDto
        {
            UserTotalPoints = totalPoints,
            WeeklyTopVotedApps = weeklyTopVotedApps,
            DiscoverHiddenGems = discoverHiddenGame
        };
    }

    public async Task<PageResultDto<AppDetailDto>> GetMadeForYouAsync(GetMadeForYouInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        var choiceList = await _discoverChoiceProvider.GetByAddressOrUserIdAsync(input.ChainId, address, userId);
        var interestedTypes = choiceList.Select(x => x.TelegramAppCategory).Distinct().ToList();
        var userInterestedAppList = await _telegramAppsProvider.GetAllDisplayAsync(new List<string>(), 4, interestedTypes);
        var madeForYouApps = ObjectMapper.Map<List<TelegramAppIndex>, List<AppDetailDto>>(userInterestedAppList);
        return new AppPageResultDto<AppDetailDto>(4, madeForYouApps.ToList());
    }

    [ExceptionHandler(typeof(Exception),  TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultThrowMethodName,Message = "Report Open App fail.", LogTargets = new []{"input"})]
    public virtual async Task<bool> OpenAppAsync(OpenAppInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();

        if (input.Alias.IsNullOrWhiteSpace())
        {
            _logger.LogWarning("Invalid Alias.");
            return false;
        }

        var telegramAppIndices = await _telegramAppsProvider.GetAllTelegramAppsAsync(new QueryTelegramAppsInput
        {
            Aliases = new List<string>() { input.Alias }
        });
        if (telegramAppIndices.IsNullOrEmpty())
        {
            _logger.LogError("Telegram App does not found");
        }

        var openedCount = await _rankingAppPointsRedisProvider.IncrementOpenedAppCountAsync(input.Alias, 1);
        await _messagePublisherService.SendOpenMessageAsync(input.ChainId, address, userId, input.Alias, 1);

        return true;
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
            await _rankingAppProvider.GetByProposalIdOrderByTotalPointsAsync(input.ChainId, proposalId, 6);
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
        var pointsPercentFactor = DoubleHelper.GetFactor((decimal)totalPoints);

        var opensDic = await _rankingAppPointsRedisProvider.GetOpenedAppCountAsync(aliasList);
        var commentsDic = await _discussionProvider.GetAppCommentCountAsync(aliasList);
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
            rankingAppDetailDto.TotalOpens = opensDic.GetValueOrDefault(rankingAppDetailDto.Alias, 0);
            rankingAppDetailDto.TotalComments = commentsDic.GetValueOrDefault(rankingAppDetailDto.Alias, 0);
        }
        return rankingList
            .Where(r => r.PointsAmount > 0) 
            .OrderByDescending(r => r.PointsAmount) 
            .ThenBy(r => aliasList.IndexOf(r.Alias))
            .Take(6)
            .ToList();
    }
    
    [ExceptionHandler(typeof(Exception),  TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultThrowMethodName,Message = "Get discover hidden gams fail.",
        LogTargets = new []{"chainId", "address", "userId"})]
    public virtual async Task<RankingAppDetailDto> GetDiscoverHiddenGame(string chainId, string address, string userId)
    {
        var choiceIndices = await _discoverChoiceProvider.GetByAddressOrUserIdAsync(chainId, address, userId);
        var categories = choiceIndices.Select(x => x.TelegramAppCategory).Distinct().ToList();
        if (categories.IsNullOrEmpty())
        {
            _logger.LogWarning("not found discover choice.");
            categories = [TelegramAppCategory.Game];
        }
        var telegramAppIndices = await _telegramAppsProvider.GetAllDisplayAsync(new List<string>(), 1000, categories);
        if (telegramAppIndices.IsNullOrEmpty())
        {
            _logger.LogWarning("not found telegram app.");
            return null;
        }

        var aliases = telegramAppIndices.Select(t => t.Alias).ToList();
        var rankingAppPointsIndices = await _rankingAppPointsProvider
            .GetRankingAppPointsIndexByAliasAsync(chainId, string.Empty, aliases, PointsType.Open);

        var telegramAppIndex = telegramAppIndices
            .GroupJoin(rankingAppPointsIndices,
                ta => ta.Alias,
                rap => rap.Alias,
                (ta, rapGroup) => new { TelegramApp = ta, Amount = rapGroup.FirstOrDefault()?.Amount ?? 0 })
            .OrderBy(x => x.Amount)
            .Select(x => x.TelegramApp).FirstOrDefault();
        
        if (telegramAppIndex == null)
        {
            return new RankingAppDetailDto();
        }
        
        var detailDto = _objectMapper.Map<TelegramAppIndex, RankingAppDetailDto>(telegramAppIndex);
        if (!telegramAppIndex.BackIcon.IsNullOrWhiteSpace())
        {
            detailDto.Icon = telegramAppIndex.BackIcon;
        }

        if (!telegramAppIndex.BackScreenshots.IsNullOrEmpty())
        {
            detailDto.Screenshots = telegramAppIndex.BackScreenshots;
        }

        return detailDto;
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
            case PointsType.ExploreSchrodinger:
                return new Tuple<string, string>("Task", "Join Schrodinger's cat");
            default:
                return new Tuple<string, string>(pointsType.ToString(), string.Empty);
        }
    }

    private async Task<List<TaskInfoDetail>> GenerateTaskInfoDetails(string chainId, string userId, string address,
        List<UserPointsIndex> taskList, UserTask userTask)
    {
        var taskDictionary = taskList
            .GroupBy(task => task.UserTaskDetail.ToString())
            .Select(g => g.OrderByDescending(task => task.PointsTime).First())
            .ToDictionary(task => task.UserTaskDetail.ToString(), task => task);
        var taskDetails = new List<TaskInfoDetail>();
        switch (userTask)
        {
            case UserTask.Daily:
                taskDetails = InitDailyTaskDetailList(await _userPointsRecordProvider.GetDailyViewAdCountAsync(chainId, userId));
                break;
            case UserTask.ExploreVotigram:
                taskDetails = InitExploreVotigramTaskDetailList();
                break;
            case UserTask.ExploreApps:
                taskDetails = InitExploreAppTaskDetailList();
                break;
            case UserTask.Referrals:
                taskDetails = InitReferralsTaskDetailList(await _referralInviteProvider.GetInviteCountAsync(chainId, address));
                break;
        }

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
            }
        };
    }

    private List<TaskInfoDetail> InitExploreVotigramTaskDetailList()
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
            }
        };
    }
    
    private List<TaskInfoDetail> InitExploreAppTaskDetailList()
    {
        return new List<TaskInfoDetail>
        {
            new()
            {
                UserTaskDetail = UserTaskDetail.ExploreSchrodinger.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreSchrodinger)
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
                UserTaskDetail = UserTaskDetail.ExploreForwardX.ToString(),
                Points = _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(PointsType.ExploreForwardX)
            }
        };
    }
    
    private List<TaskInfoDetail> InitReferralsTaskDetailList(long completeCount)
    {
        return new List<TaskInfoDetail>
        {
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

    private async Task<bool> CheckSchrodinger(string chainId, string address)
    {
        var completed = await _userPointsRecordProvider.GetUserTaskCompleteAsync(chainId, address, UserTask.Explore,
            UserTaskDetail.ExploreSchrodinger);
        if (completed)
        {
            return true;
        }
        var userInfo = await _telegramUserInfoProvider.GetByAddressAsync(address);
        var id = userInfo?.TelegramId ?? string.Empty;
        var complete = await _schrodingerApiProvider.CheckAsync(id);
        _logger.LogInformation($"CheckSchrodinger id {id} address {address} complete {complete}");
        return complete;
    }
}