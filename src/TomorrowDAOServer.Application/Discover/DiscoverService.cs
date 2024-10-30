using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Discover.Dto;
using TomorrowDAOServer.Discover.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;

namespace TomorrowDAOServer.Discover;

public class DiscoverService : ApplicationService, IDiscoverService
{
    private readonly IDiscoverChoiceProvider _discoverChoiceProvider;
    private readonly IUserProvider _userProvider;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IRankingAppPointsProvider _rankingAppPointsProvider;
    private readonly IUserViewAppProvider _userViewAppProvider;

    public DiscoverService(IDiscoverChoiceProvider discoverChoiceProvider, IUserProvider userProvider,
        ITelegramAppsProvider telegramAppsProvider, IRankingAppPointsProvider rankingAppPointsProvider, 
        IUserViewAppProvider userViewAppProvider)
    {
        _discoverChoiceProvider = discoverChoiceProvider;
        _userProvider = userProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _rankingAppPointsProvider = rankingAppPointsProvider;
        _userViewAppProvider = userViewAppProvider;
    }

    public async Task<bool> DiscoverViewedAsync(string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        return await _discoverChoiceProvider.DiscoverViewedAsync(chainId, address);
    }

    public async Task<bool> DiscoverChooseAsync(string chainId, List<string> choices)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var choiceEnums = CheckCategories(choices);
        var exists = await _discoverChoiceProvider.GetExistByAddressAndDiscoverTypeAsync(chainId, address, DiscoverChoiceType.Choice);
        if (exists)
        {
            throw new UserFriendlyException("Already chose the discover type.");
        }

        var toAdd = choiceEnums.Select(category => new DiscoverChoiceIndex
            {
                Id = GuidHelper.GenerateGrainId(chainId, address, category.ToString(), DiscoverChoiceType.Choice.ToString()),
                ChainId = chainId,
                Address = address,
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
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        var res =  input.Category switch
        {
            CommonConstant.Recommend => await GetRecommendAppListAsync(input, address),
            CommonConstant.New => await GetNewAppListAsync(input, address),
            _ => await GetCategoryAppListAsync(input)
        };
        await FillTotalPoints(input.ChainId, res.Data);
        if (CommonConstant.New != input.Category && input.SkipCount == 0)
        {
            var (notViewedNewAppCount, _, _) = await GetNewAppInfo(input, address);
            res.NotViewedNewAppCount = notViewedNewAppCount;
        }
        return res;
    }

    public async Task<bool> ViewAppAsync(ViewAppInput input)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        if (input.Aliases.IsNullOrEmpty())
        {
            return true;
        }
        var (count, apps) = await _telegramAppsProvider
            .GetTelegramAppsAsync(new QueryTelegramAppsInput { Aliases = input.Aliases });
        if (count == 0)
        {
            return true;
        }
        var viewApp = await _userViewAppProvider.GetByAddress(address);
        var aliasesList = new List<string>((viewApp?.AliasesString ?? string.Empty).Split(CommonConstant.Comma));
        aliasesList.AddRange(apps.Select(x => x.Alias).ToList());
        aliasesList = aliasesList.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        await _userViewAppProvider.AddOrUpdateAsync(new UserViewAppIndex
        {
            Id = GuidHelper.GenerateGrainId(input.ChainId, address), ChainId = input.ChainId, Address = address,
            AliasesString = JsonConvert.SerializeObject(aliasesList)
        });
        return true;
    }

    private async Task<Tuple<int?, List<TelegramAppIndex>, HashSet<string>>> GetNewAppInfo(GetDiscoverAppListInput input, string address)
    {
        var latest = await _telegramAppsProvider.GetLatestCreatedAsync();
        var viewApp = await _userViewAppProvider.GetByAddress(address);
        var viewAliases = new HashSet<string>(JsonConvert.DeserializeObject<List<string>>(viewApp?.AliasesString ?? string.Empty));
        var createTime = latest.CreateTime;
        var monthStart = new DateTime(createTime.Year, createTime.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var newAppList = await _telegramAppsProvider.GetAllByTimePeriodAsync(monthStart, monthEnd);
        var notViewedNewAppCount = input.SkipCount == 0 ? newAppList.Count(app => !viewAliases.Contains(app.Alias)) : (int?)null;
        return new Tuple<int?, List<TelegramAppIndex>, HashSet<string>>(notViewedNewAppCount, newAppList, viewAliases);
    }
    
    private async Task<AppPageResultDto<DiscoverAppDto>> GetNewAppListAsync(GetDiscoverAppListInput input, string address)
    {
        var (notViewedNewAppCount, newAppList, viewAliases) = await GetNewAppInfo(input, address);
        var newApps = ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(
            newAppList.OrderByDescending(x => x.CreateTime).Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
        foreach (var app in newApps)
        {
            app.Viewed = viewAliases.Contains(app.Alias);
        }
        return new AppPageResultDto<DiscoverAppDto>
        {
            TotalCount = newAppList.Count, Data = newApps, NotViewedNewAppCount = notViewedNewAppCount
        };
    }

    private async Task<AppPageResultDto<DiscoverAppDto>> GetRecommendAppListAsync(GetDiscoverAppListInput input, string address)
    {
        var choiceList = await _discoverChoiceProvider.GetByAddressAsync(input.ChainId, address);
        var types = choiceList.Select(x => x.TelegramAppCategory).Distinct().ToList();
        var appList = (await _telegramAppsProvider.GetAllDisplayAsync(input.Aliases))
            .Where(x => x.Categories is { Count: > 0 }).ToList();
        var userInterestedAppList = appList.Where(app => types.Intersect(app.Categories).Any()).ToList();
        var userNotInterestedAppList = appList.Where(app => !types.Intersect(app.Categories).Any()).ToList();
        var recommendApps = new List<DiscoverAppDto>();
        var interestedCount = (int)(input.MaxResultCount * CommonConstant.InterestedPercent);
        var notInterestedCount = input.MaxResultCount - interestedCount;
        AddRandomApps(userInterestedAppList, interestedCount, recommendApps);
        AddRandomApps(userNotInterestedAppList, notInterestedCount, recommendApps);
        if (recommendApps.Count < input.MaxResultCount)
        {
            var remainingCount = input.MaxResultCount - recommendApps.Count;
            var remainingApps = appList.Where(app => !recommendApps.Select(r => r.Alias).Contains(app.Alias)).ToList();
            AddRandomApps(remainingApps, remainingCount, recommendApps);
        }
        return new AppPageResultDto<DiscoverAppDto>
        {
            TotalCount = appList.Count + input.Aliases?.Count ?? 0, Data = recommendApps.ToList()
        };
    }
    
    private async Task<AppPageResultDto<DiscoverAppDto>> GetCategoryAppListAsync(GetDiscoverAppListInput input)
    {
        var category = CheckCategory(input.Category);
        var (totalCount, appList) = await _telegramAppsProvider.GetByCategoryAsync(category, input.SkipCount, input.MaxResultCount);
        var categoryApps = ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(appList);
        return new AppPageResultDto<DiscoverAppDto>
        {
            TotalCount = totalCount, Data = categoryApps
        };
    }
    
    private void AddRandomApps(IReadOnlyCollection<TelegramAppIndex> appList, int count, List<DiscoverAppDto> targetList)
    {
        if (appList.Count > 0)
        {
            targetList.AddRange(appList
                .OrderBy(_ => Guid.NewGuid()).Take(count)
                .Select(app => ObjectMapper.Map<TelegramAppIndex, DiscoverAppDto>(app))  
            );
        }
    }


    private async Task FillTotalPoints(string chainId, List<DiscoverAppDto> list)
    {
        var aliases = list.Select(x => x.Alias).ToList();
        var pointsDic = await _rankingAppPointsProvider.GetTotalPointsByAliasAsync(chainId, aliases);
        foreach (var app in list)
        {
            app.TotalPoints = pointsDic.GetValueOrDefault(app.Alias, 0);
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