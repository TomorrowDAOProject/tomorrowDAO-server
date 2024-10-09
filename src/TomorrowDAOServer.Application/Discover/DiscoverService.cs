using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Discover.Dto;
using TomorrowDAOServer.Discover.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Ranking.Provider;
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

    public DiscoverService(IDiscoverChoiceProvider discoverChoiceProvider, IUserProvider userProvider,
        ITelegramAppsProvider telegramAppsProvider, IRankingAppPointsProvider rankingAppPointsProvider)
    {
        _discoverChoiceProvider = discoverChoiceProvider;
        _userProvider = userProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _rankingAppPointsProvider = rankingAppPointsProvider;
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

    public async Task<List<DiscoverAppDto>> GetDiscoverAppListAsync(GetDiscoverAppListInput input)
    {
        if (input.Category == CommonConstant.Recommend)
        {
            return await GetRecommendAppListAsync(input);
        }

        return await GetCategoryAppListAsync(input);
    }

    private async Task<List<DiscoverAppDto>> GetRecommendAppListAsync(GetDiscoverAppListInput input)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        var choiceList = await _discoverChoiceProvider.GetByAddressAsync(input.ChainId, address);
        var types = choiceList.Select(x => x.TelegramAppCategory).Distinct().ToList();
        var appList = (await _telegramAppsProvider.GetAllHasUrlAsync())
            .Where(x => !string.IsNullOrEmpty(x.Url)).ToList();
        var userInterestedAppList = appList.Where(app => app.Categories != null &&  types.Intersect(app.Categories).Any()).ToList();
        var userNotInterestedAppList = appList.Where(app => app.Categories != null && !types.Intersect(app.Categories).Any()).ToList();
        var recommendApps = new List<DiscoverAppDto>();
        var interestedCount = (int)(input.MaxResultCount * CommonConstant.InterestedPercent);
        var notInterestedCount = input.MaxResultCount - interestedCount;
        AddRandomApps(userInterestedAppList, interestedCount, recommendApps);
        AddRandomApps(userNotInterestedAppList, notInterestedCount, recommendApps);
        if (recommendApps.Count < input.MaxResultCount)
        {
            var remainingCount = input.MaxResultCount - recommendApps.Count;
            var remainingApps = appList.Where(app => !recommendApps.Select(r => r.Alias).Contains(app.Id)).ToList();
            AddRandomApps(remainingApps, remainingCount, recommendApps);
        }
        await FillTotalPoints(input.ChainId, recommendApps);
        return recommendApps.ToList();
    }
    
    private async Task<List<DiscoverAppDto>> GetCategoryAppListAsync(GetDiscoverAppListInput input)
    {
        var category = CheckCategory(input.Category);
        var appList = await _telegramAppsProvider.GetByCategoryAsync(category, input.SkipCount, input.MaxResultCount);
        var categoryApps = ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(appList);
        await FillTotalPoints(input.ChainId, categoryApps);
        return categoryApps;
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