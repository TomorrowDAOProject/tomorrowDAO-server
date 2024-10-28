using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
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
    private readonly IUserViewNewAppProvider _userViewNewAppProvider;

    public DiscoverService(IDiscoverChoiceProvider discoverChoiceProvider, IUserProvider userProvider,
        ITelegramAppsProvider telegramAppsProvider, IRankingAppPointsProvider rankingAppPointsProvider, 
        IUserViewNewAppProvider userViewNewAppProvider)
    {
        _discoverChoiceProvider = discoverChoiceProvider;
        _userProvider = userProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _rankingAppPointsProvider = rankingAppPointsProvider;
        _userViewNewAppProvider = userViewNewAppProvider;
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

    public async Task<PageResultDto<DiscoverAppDto>> GetDiscoverAppListAsync(GetDiscoverAppListInput input)
    {
        return input.Category switch
        {
            CommonConstant.Recommend => await GetRecommendAppListAsync(input),
            CommonConstant.New => await GetNewAppListAsync(input),
            _ => await GetCategoryAppListAsync(input)
        };
    }

    public async Task<long> ViewAppAsync(ViewAppInput input)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        var latest = await _telegramAppsProvider.GetLatestCreatedAsync();
        if (latest == null)
        {
            return 0;
        }
        var createTime = latest.CreateTime;
        var monthStart = new DateTime(createTime.Year, createTime.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var newApps = await _telegramAppsProvider.GetAllByTimePeriodAsync(monthStart, monthEnd);
        var newAppAliases = newApps.Select(x => x.Alias).ToList();
        var intersection = newAppAliases.Intersect(input.Aliases).ToList();
        
        var viewApp = await _userViewNewAppProvider.GetByAddressAndTime(address);
        var aliasesList = viewApp?.AliasesList ?? new List<string>();
        aliasesList.AddRange(intersection);
        await _userViewNewAppProvider.AddOrUpdateAsync(new UserViewNewAppIndex
        {
            Id = GuidHelper.GenerateGrainId(input.ChainId, address), ChainId = input.ChainId, Address = address,
            AliasesList = aliasesList.Distinct().ToList()
        });
        
        return newAppAliases.Except(aliasesList).Count();
    }

    private async Task<PageResultDto<DiscoverAppDto>> GetNewAppListAsync(GetDiscoverAppListInput input)
    {
        var latest = await _telegramAppsProvider.GetLatestCreatedAsync();
        if (latest == null)
        {
            return new PageResultDto<DiscoverAppDto>();
        }

        var createTime = latest.CreateTime;
        var monthStart = new DateTime(createTime.Year, createTime.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var (count, appList) = await _telegramAppsProvider.GetByTimePeriodAsync(monthStart, monthEnd, input.SkipCount, input.MaxResultCount);
        var newApps = ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(appList);
        await FillTotalPoints(input.ChainId, newApps);
        return new PageResultDto<DiscoverAppDto>
        {
            TotalCount = count, Data = newApps
        };
    }

    private async Task<PageResultDto<DiscoverAppDto>> GetRecommendAppListAsync(GetDiscoverAppListInput input)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
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
        await FillTotalPoints(input.ChainId, recommendApps);
        return new PageResultDto<DiscoverAppDto>
        {
            TotalCount = appList.Count + input.Aliases?.Count ?? 0, Data = recommendApps.ToList()
        };
    }
    
    private async Task<PageResultDto<DiscoverAppDto>> GetCategoryAppListAsync(GetDiscoverAppListInput input)
    {
        var category = CheckCategory(input.Category);
        var (totalCount, appList) = await _telegramAppsProvider.GetByCategoryAsync(category, input.SkipCount, input.MaxResultCount);
        var categoryApps = ObjectMapper.Map<List<TelegramAppIndex>, List<DiscoverAppDto>>(appList);
        await FillTotalPoints(input.ChainId, categoryApps);
        return new PageResultDto<DiscoverAppDto>
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