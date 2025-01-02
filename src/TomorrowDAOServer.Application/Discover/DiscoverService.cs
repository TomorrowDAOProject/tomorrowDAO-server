using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.Security;
using TomorrowDAOServer.Discover.Dto;
using TomorrowDAOServer.Discover.Provider;
using TomorrowDAOServer.Discussion.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User.Provider;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;

namespace TomorrowDAOServer.Discover;

public class DiscoverService : ApplicationService, IDiscoverService
{
    private readonly ILogger<DiscoverService> _logger;
    private readonly IDiscoverChoiceProvider _discoverChoiceProvider;
    private readonly IUserProvider _userProvider;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IRankingAppPointsProvider _rankingAppPointsProvider;
    private readonly IUserViewAppProvider _userViewAppProvider;
    private readonly IOptionsMonitor<DiscoverOptions> _discoverOptions;
    private readonly IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;
    private readonly IDiscussionProvider _discussionProvider;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private readonly IVoteProvider _voteProvider;
    private readonly IDistributedCache<string> _distributedCache;

    public DiscoverService(IDiscoverChoiceProvider discoverChoiceProvider, IUserProvider userProvider,
        ITelegramAppsProvider telegramAppsProvider, IRankingAppPointsProvider rankingAppPointsProvider,
        IUserViewAppProvider userViewAppProvider, IOptionsMonitor<DiscoverOptions> discoverOptions,
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, IDiscussionProvider discussionProvider,
        IRankingAppProvider rankingAppProvider, IProposalProvider proposalProvider,
        IOptionsMonitor<RankingOptions> rankingOptions, IVoteProvider voteProvider, ILogger<DiscoverService> logger, 
        IDistributedCache<string> distributedCache)
    {
        _discoverChoiceProvider = discoverChoiceProvider;
        _userProvider = userProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _rankingAppPointsProvider = rankingAppPointsProvider;
        _userViewAppProvider = userViewAppProvider;
        _discoverOptions = discoverOptions;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _discussionProvider = discussionProvider;
        _rankingAppProvider = rankingAppProvider;
        _proposalProvider = proposalProvider;
        _rankingOptions = rankingOptions;
        _voteProvider = voteProvider;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<bool> DiscoverViewedAsync(string chainId)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(chainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        return await _discoverChoiceProvider.DiscoverViewedAsync(chainId, address, userId);
    }

    public async Task<bool> DiscoverChooseAsync(string chainId, List<string> choices)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(chainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();

        var choiceEnums = CheckCategories(choices);
        var exists =
            await _discoverChoiceProvider.GetExistByAddressAndUserIdAndDiscoverTypeAsync(chainId, userId, address,
                DiscoverChoiceType.Choice);
        if (exists)
        {
            throw new UserFriendlyException("Already chose the discover type.");
        }

        var toAdd = choiceEnums.Select(category => new DiscoverChoiceIndex
            {
                Id = GuidHelper.GenerateGrainId(chainId, address, category.ToString(),
                    DiscoverChoiceType.Choice.ToString()),
                ChainId = chainId,
                Address = address ?? string.Empty,
                UserId = userId,
                TelegramAppCategory = category,
                DiscoverChoiceType = DiscoverChoiceType.Choice,
                UpdateTime = DateTime.UtcNow
            })
            .ToList();

        await _discoverChoiceProvider.BulkAddOrUpdateAsync(toAdd);
        return true;
    }

    public async Task<AppPageResultDto<DiscoverAppDto>> GetDiscoverAppListAsync(GetDiscoverAppListInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        var res = input.Category switch
        {
            CommonConstant.New => await GetNewAppListAsync(input, address, userId),
            _ => await GetCategoryAppListAsync(input, _discoverOptions.CurrentValue.TopApps, string.Empty)
        };
        await FillData(input.ChainId, res.Data);
        return res;
    }

    public async Task<RandomAppListDto> GetRandomAppListAsync(GetRandomAppListInputAsync input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        var res = input.Category switch
        {
            CommonConstant.Recommend => await GetRecommendAppListAsync(input, address, userId),
            CommonConstant.ForYou => await GetForYouAppListAsync(input, address, userId),
            _ => throw new UserFriendlyException($"Invalid category {input.Category}.")
        };
        await FillData(input.ChainId, res.AppList);
        return res;
    }

    public async Task<AccumulativeAppPageResultDto<DiscoverAppDto>> GetAccumulativeAppListAsync(
        GetDiscoverAppListInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        var res = await GetCategoryAppListAsync(input, new List<string>(), "TotalPoints");
        var allPoints = await _telegramAppsProvider.GetTotalPointsAsync();
        PointsPercent(allPoints, res.Data);
        await FillData(input.ChainId, res.Data, false);
        return new AccumulativeAppPageResultDto<DiscoverAppDto>
        {
            Data = res.Data.OrderBy(x => x.Title).ToList(), TotalCount = res.TotalCount,
            UserTotalPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(userId, address)
        };
    }

    public async Task<CurrentAppPageResultDto<DiscoverAppDto>> GetCurrentAppListAsync(GetDiscoverAppListInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        var topRankingAddress = _rankingOptions.CurrentValue.TopRankingAddress;
        var proposal = await _proposalProvider.GetTopProposalAsync(topRankingAddress, true);
        if (proposal == null)
        {
            return new CurrentAppPageResultDto<DiscoverAppDto>();
        }

        var proposalId = proposal.ProposalId;
        var search = input.Search;
        var (total, rankingAppList) = await _rankingAppProvider.GetRankingAppListAsync(new GetRankingAppListInput
        {
            MaxResultCount = input.MaxResultCount,
            SkipCount = input.SkipCount,
            ChainId = input.ChainId,
            Category = input.Category,
            Search = input.Search,
            ProposalId = proposalId
        });
        var list = ObjectMapper.Map<List<RankingAppIndex>, List<DiscoverAppDto>>(rankingAppList);
        // if (!string.IsNullOrEmpty(input.Category))
        // {
        //     var category = CheckCategory(input.Category);
        //     list = list.Where(x => x.Categories.Contains(category.ToString())).ToList();
        // }
        var allPoints = await _rankingAppPointsRedisProvider.GetProposalPointsAsync(proposalId);
        // if (!string.IsNullOrEmpty(search))
        // {
        //     list = list.Where(x => x.Title != null && x.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
        //         .ToList();
        // }

        await FillData(input.ChainId, list, false);
        PointsPercent(allPoints, list);
        //list = list.OrderByDescending(x => x.TotalPoints).ToList();
        var votingRecord = await GetRankingVoteRecordAsync(input.ChainId, address, proposalId, input.Category);

        return new CurrentAppPageResultDto<DiscoverAppDto>
        {
            TotalCount = list.Count, ProposalId = proposalId, Data = list.OrderByDescending(x => x.TotalPoints).ThenBy(x => x.Title).ToList(),
            ActiveEndEpochTime = rankingAppList?.FirstOrDefault()?.ActiveEndTime.ToUtcMilliSeconds() ?? 0,
            UserTotalPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(userId, address),
            CanVote = votingRecord == null
        };
    }

    public async Task<bool> ViewAppAsync(ViewAppInput input)
    {
        if (input.Aliases.IsNullOrEmpty())
        {
            return true;
        }

        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        var (_, apps) = await _telegramAppsProvider
            .GetTelegramAppsAsync(new QueryTelegramAppsInput { Aliases = input.Aliases }, true);
        apps = apps.Where(x => x.SourceType != SourceType.TomorrowDao).ToList();
        if (!apps.Any())
        {
            return true;
        }

        var aliases = apps.Select(x => x.Alias).Distinct().ToList();
        var toAdd = aliases.Select(alias => new UserViewedAppIndex
        {
            Id = GuidHelper.GenerateGrainId(address, alias), Alias = alias, Address = address ?? string.Empty,
            UserId = userId
        }).ToList();

        await _userViewAppProvider.BulkAddOrUpdateAsync(toAdd);
        return true;
    }

    public async Task<RankingVoteRecord> GetRankingVoteRecordAsync(string chainId, string address, string proposalId, string category)
    {
        var distributeCacheKey = RedisHelper.GenerateDistributeCacheKey(chainId, address, proposalId, category);
        var cache = await _distributedCache.GetAsync(distributeCacheKey);
        return cache.IsNullOrWhiteSpace() ? null : JsonConvert.DeserializeObject<RankingVoteRecord>(cache);
    }
    private async Task<AppPageResultDto<DiscoverAppDto>> GetSearchAppListAsync(GetDiscoverAppListInput input)
    {
        TelegramAppCategory? category = Enum.TryParse(input.Category, true, out TelegramAppCategory categoryEnum)
            ? categoryEnum
            : null;
        var (count, list) =
            await _telegramAppsProvider.GetSearchListAsync(category, input.Search, input.SkipCount,
                input.MaxResultCount);
        var searchApps = ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(list);
        return new AppPageResultDto<DiscoverAppDto>(count, searchApps);
    }

    private async Task<AppPageResultDto<DiscoverAppDto>> GetNewAppListAsync(GetDiscoverAppListInput input,
        string address, string userId)
    {
        var latest = await _telegramAppsProvider.GetLatestCreatedAsync();
        if (latest == null)
        {
            return new AppPageResultDto<DiscoverAppDto>(0, new List<DiscoverAppDto>(), 0);
        }

        var search = input.Search;
        var createTime = latest.CreateTime;
        var start = createTime.AddDays(-30);
        var newAppList = (await _telegramAppsProvider.GetAllByTimePeriodAsync(start, createTime))
            .OrderByDescending(x => x.CreateTime).Take(100).ToList();
        if (!string.IsNullOrEmpty(search))
        {
            newAppList = newAppList
                .Where(x => x.Title != null && x.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var aliases = newAppList.Select(x => x.Alias).Distinct().ToList();
        var viewedApps = await _userViewAppProvider.GetByAliasList(userId, address, aliases);
        var viewedAliases = viewedApps.Select(x => x.Alias).ToList();
        var notViewedNewAppCount = input.SkipCount == 0 ? aliases.Except(viewedAliases).Count() : 0;
        var newApps = ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(newAppList);
        foreach (var app in newApps)
        {
            app.Viewed = viewedAliases.Contains(app.Alias);
        }

        var sortedNewApps = newApps.OrderBy(app => app.Viewed)
            .ThenByDescending(app => app.CreateTime).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new AppPageResultDto<DiscoverAppDto>(newAppList.Count, sortedNewApps, notViewedNewAppCount);
    }

    private async Task<RandomAppListDto> GetForYouAppListAsync(GetRandomAppListInputAsync input, string address,
        string userId)
    {
        input.MaxResultCount = 16;
        var choiceList = await _discoverChoiceProvider.GetByAddressOrUserIdAsync(input.ChainId, address, userId);
        var adUrlList = _discoverOptions.CurrentValue.AdUrls;
        var allCategories = Enum.GetValues(typeof(TelegramAppCategory)).Cast<TelegramAppCategory>().ToList();
        var interestedTypes = choiceList.Select(x => x.TelegramAppCategory).Distinct().ToList();
        var notInterestedTypes = allCategories.Where(category => !interestedTypes.Contains(category)).ToList();
        List<DiscoverAppDto> appList;
        if (interestedTypes.Count == 1)
        {
            appList = (await GetRecommendAppListAsync(input, interestedTypes, notInterestedTypes)).AppList;
        }
        else
        {
            var excludeAliases = input.Aliases ?? new List<string>();
            var interestedCount = (int)(input.MaxResultCount * CommonConstant.InterestedPercent);
            var userInterestedAppList =
                await _telegramAppsProvider.GetAllDisplayAsync(excludeAliases, interestedCount, interestedTypes);
            appList = ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(userInterestedAppList);
        }

        var random = new Random();
        for (var i = 8; i < appList.Count; i += 8 + 1)
        {
            var randomUrl = adUrlList[random.Next(adUrlList.Count)];
            appList.Insert(i, new DiscoverAppDto { AppType = "AD", Url = randomUrl });
        }

        return new RandomAppListDto
        {
            AppList = appList
        };
    }

    private async Task<RandomAppListDto> GetRecommendAppListAsync(GetRandomAppListInputAsync input, string address,
        string userId)
    {
        var choiceList = await _discoverChoiceProvider.GetByAddressOrUserIdAsync(input.ChainId, address, userId);
        var allCategories = Enum.GetValues(typeof(TelegramAppCategory)).Cast<TelegramAppCategory>().ToList();
        var interestedTypes = choiceList.Select(x => x.TelegramAppCategory).Distinct().ToList();
        var notInterestedTypes = allCategories.Where(category => !interestedTypes.Contains(category)).ToList();
        return await GetRecommendAppListAsync(input, interestedTypes, notInterestedTypes);
    }

    private async Task<RandomAppListDto> GetRecommendAppListAsync(GetRandomAppListInputAsync input,
        List<TelegramAppCategory> interestedTypes, List<TelegramAppCategory> notInterestedTypes)
    {
        var excludeAliases = input.Aliases ?? new List<string>();
        var interestedCount = (int)(input.MaxResultCount * CommonConstant.InterestedPercent);
        var notInterestedCount = input.MaxResultCount - interestedCount;
        var userInterestedAppList =
            await _telegramAppsProvider.GetAllDisplayAsync(excludeAliases, interestedCount, interestedTypes);
        var userNotInterestedAppList =
            await _telegramAppsProvider.GetAllDisplayAsync(excludeAliases, notInterestedCount, notInterestedTypes);
        var recommendApps = new List<DiscoverAppDto>();
        recommendApps.AddRange(ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(userInterestedAppList));
        recommendApps.AddRange(
            ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(userNotInterestedAppList));
        if (recommendApps.Count < input.MaxResultCount)
        {
            var remainingCount = input.MaxResultCount - recommendApps.Count;
            var chooseAliases = recommendApps.Select(x => x.Alias).ToList();
            excludeAliases.AddRange(chooseAliases);
            var remainingApps = await _telegramAppsProvider.GetAllDisplayAsync(excludeAliases, remainingCount);
            recommendApps.AddRange(ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(remainingApps));
        }

        return new RandomAppListDto { AppList = recommendApps };
    }

    private async Task<AppPageResultDto<DiscoverAppDto>> GetCategoryAppListAsync(GetDiscoverAppListInput input,
        List<string> aliases, string sort)
    {
        TelegramAppCategory? category = string.IsNullOrEmpty(input.Category) ? null : CheckCategory(input.Category);
        var search = input.Search;
        var topApps = new List<TelegramAppIndex>();
        if (!string.IsNullOrEmpty(search))
        {
            return await GetSearchAppListAsync(input);
        }

        if (!aliases.IsNullOrEmpty())
        {
            var (_, list) =
                await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput { Aliases = aliases });
            if (category == null)
            {
                topApps = list;
            }
            else
            {
                topApps = list.Where(x => x.Categories.Contains(category.Value))
                    .OrderBy(app => aliases.IndexOf(app.Alias)).ToList();
            }
        }

        var availableTopApps = topApps.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
        var availableTopAppCount = availableTopApps.Count;
        if (availableTopAppCount < input.MaxResultCount)
        {
            var remainingCount = input.MaxResultCount - availableTopAppCount;
            var (totalCount, additionalApps) = await _telegramAppsProvider.GetByCategoryAsync(
                category, Math.Max(0, input.SkipCount - topApps.Count), remainingCount, aliases, sort);
            availableTopApps.AddRange(additionalApps);
            var discoverAppDtos = new List<DiscoverAppDto>();
            foreach (var telegramAppIndex in availableTopApps)
            {
                var discoverAppDto = ObjectMapper.Map<TelegramAppIndex, DiscoverAppDto>(telegramAppIndex);
                if (!telegramAppIndex.BackIcon.IsNullOrWhiteSpace())
                {
                    discoverAppDto.Icon = telegramAppIndex.BackIcon;
                }

                if (!telegramAppIndex.BackScreenshots.IsNullOrEmpty())
                {
                    discoverAppDto.Screenshots = telegramAppIndex.BackScreenshots;
                }

                discoverAppDtos.Add(discoverAppDto);
            }

            return new AppPageResultDto<DiscoverAppDto>(totalCount + topApps.Count, discoverAppDtos);
        }

        var count = await _telegramAppsProvider.CountByCategoryAsync(category);
        return new AppPageResultDto<DiscoverAppDto>(count,
            ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(availableTopApps));
    }

    private async Task<PageResultDto<DiscoverAppDto>> GetCurrentAppListAsync(GetVoteAppListInput input)
    {
        var topRankingAddress = _rankingOptions.CurrentValue.TopRankingAddress;
        var proposal = await _proposalProvider.GetTopProposalAsync(topRankingAddress, true);
        if (proposal == null)
        {
            return new PageResultDto<DiscoverAppDto>();
        }

        var proposalId = proposal.ProposalId;
        var list = await _rankingAppProvider.GetByProposalIdAsync(input.ChainId, proposalId);
        return new PageResultDto<DiscoverAppDto>
        {
            TotalCount = list.Count, Data = ObjectMapper.Map<List<RankingAppIndex>, List<DiscoverAppDto>>(list)
        };
    }

    private async Task FillData(string chainId, List<DiscoverAppDto> list, bool flag = true)
    {
        var aliases = list.Where(x => !string.IsNullOrEmpty(x.Alias)).Select(x => x.Alias).Distinct().ToList();
        var pointsDic = await _rankingAppPointsProvider.GetTotalPointsByAliasAsync(chainId, aliases);
        var opensDic = await _rankingAppPointsRedisProvider.GetOpenedAppCountAsync(aliases);
        var likesDic = await _rankingAppPointsRedisProvider.GetAppLikeCountAsync(aliases);
        var commentsDic = await _discussionProvider.GetAppCommentCountAsync(aliases);
        foreach (var app in list.Where(x => !string.IsNullOrWhiteSpace(x.Alias)))
        {
            if (flag)
            {
                app.TotalPoints = pointsDic.GetValueOrDefault(app.Alias, 0);
                app.TotalLikes = likesDic.GetValueOrDefault(app.Alias, 0);
            }
            app.TotalOpens = opensDic.GetValueOrDefault(app.Alias, 0);
            app.TotalComments = commentsDic.GetValueOrDefault(app.Alias, 0);
        }
    }

    private static void PointsPercent(long sum, List<DiscoverAppDto> list)
    {
        var factor = DoubleHelper.GetFactor((decimal)sum);
        foreach (var app in list)
        {
            app.PointsPercent = app.TotalPoints * factor;
        }
    }

    private static List<TelegramAppCategory> CheckCategories(List<string> categories)
    {
        return categories.Select(CheckCategory).ToList();
    }

    private static TelegramAppCategory CheckCategory(string category)
    {
        if (Enum.TryParse<TelegramAppCategory>(category, true, out var categoryEnum))
        {
            return categoryEnum;
        }

        throw new UserFriendlyException($"Invalid category {category}.");
    }
}