using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace TomorrowDAOServer.Telegram;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TelegramService : TomorrowDAOServerAppService, ITelegramService
{
    private readonly ILogger<TelegramService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IUserProvider _userProvider;
    private readonly IOptionsMonitor<TelegramOptions> _telegramOptions;
    private readonly IDaoAliasProvider _daoAliasProvider;

    public TelegramService(ILogger<TelegramService> logger, IObjectMapper objectMapper,
        ITelegramAppsProvider telegramAppsProvider, IUserProvider userProvider,
        IOptionsMonitor<TelegramOptions> telegramOptions, IDaoAliasProvider daoAliasProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _telegramAppsProvider = telegramAppsProvider;
        _userProvider = userProvider;
        _telegramOptions = telegramOptions;
        _daoAliasProvider = daoAliasProvider;
    }

    public async Task SetCategoryAsync(string chainId)
    {
        await CheckAddress(chainId);
        var types = _telegramOptions.CurrentValue.Types;
        if (types.IsNullOrEmpty())
        {
            var allCategories = Enum.GetValues(typeof(TelegramAppCategory)).Cast<TelegramAppCategory>().ToList();
            var random = new Random();

            var all = await _telegramAppsProvider.GetAllAsync();

            foreach (var app in all)
            {
                var randomCategories = allCategories.OrderBy(x => random.Next()).Take(random.Next(1, 4)).ToList();
                app.Categories = randomCategories; 
            }

            await _telegramAppsProvider.BulkAddOrUpdateAsync(all);
            return;
        }
        
        var typesDic = ParseTypes(types.Split(CommonConstant.Comma));
        var aliases = typesDic.Keys.ToList();
        var exists = (await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
        {
            Aliases = aliases
        })).Item2;
        foreach (var app in exists)
        {
            if (typesDic.TryGetValue(app.Alias, out var category))
            {
                app.Categories = category; 
            }
        }
        await _telegramAppsProvider.BulkAddOrUpdateAsync(exists);
    }
    
    private Dictionary<string, List<TelegramAppCategory>> ParseTypes(IEnumerable<string> types)
    {
        var result = new Dictionary<string, List<TelegramAppCategory>>();
    
        foreach (var parts in types.Select(type => type.Split(CommonConstant.Colon)))
        {
            if (parts.Length != 2)
            {
                continue;
            }

            var categories = parts[1].Split(CommonConstant.Add)
                .Select(cat => cat.Trim())
                .Where(cat => Enum.TryParse(cat, out TelegramAppCategory category))
                .Select(cat => (TelegramAppCategory)Enum.Parse(typeof(TelegramAppCategory), cat))
                .ToList();
            var alias = parts[0].Trim();
            if (result.TryGetValue(alias, out var existingCategories))
            {
                existingCategories.AddRange(categories.Where(cat => !existingCategories.Contains(cat)));
            }
            else
            {
                result.Add(alias, categories);
            }
        }

        return result;
    }

    public async Task SaveTelegramAppAsync(TelegramAppDto telegramAppDto, string chainId)
    {
        if (telegramAppDto == null || telegramAppDto.Title.IsNullOrWhiteSpace() || chainId.IsNullOrWhiteSpace())
        {
            return;
        }

        await CheckAddress(chainId);

        try
        {
            var telegramAppIndex = _objectMapper.Map<TelegramAppDto, TelegramAppIndex>(telegramAppDto);
            if (telegramAppIndex.Alias.IsNullOrWhiteSpace())
            {
                telegramAppIndex.Alias = await _daoAliasProvider.GenerateDaoAliasAsync(telegramAppDto.Title);
            }

            if (telegramAppIndex.Id.IsNullOrWhiteSpace())
            {
                telegramAppIndex.Id = HashHelper.ComputeFrom(telegramAppDto.Title).ToHex();
            }
            
            await _telegramAppsProvider.AddOrUpdateAsync(telegramAppIndex);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SaveTelegramAppAsync error. {0}", JsonConvert.SerializeObject(telegramAppDto));
            throw new UserFriendlyException($"System exception occurred during saving telegram app. {e.Message}");
        }
    }

    public async Task SaveTelegramAppsAsync(List<TelegramAppDto> telegramAppDtos)
    {
        if (telegramAppDtos.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            var telegramAppIndices = _objectMapper.Map<List<TelegramAppDto>, List<TelegramAppIndex>>(telegramAppDtos);
            await _telegramAppsProvider.BulkAddOrUpdateAsync(telegramAppIndices);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SaveTelegramAppsAsync error. {0}", JsonConvert.SerializeObject(telegramAppDtos));
            throw new UserFriendlyException($"System exception occurred during saving telegram apps. {e.Message}");
        }
    }

    public async Task SaveNewTelegramAppsAsync(List<TelegramAppDto> telegramAppDtos)
    {
        if (telegramAppDtos.IsNullOrEmpty())
        {
            return;
        }
        var telegramAppIndices = _objectMapper.Map<List<TelegramAppDto>, List<TelegramAppIndex>>(telegramAppDtos);
        var aliases = telegramAppIndices.Select(x => x.Alias).ToList();
        var exists = (await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
        {
            Aliases = aliases
        })).Item2;
        var toUpdate = telegramAppIndices.Where(x => exists.All(y => x.Id != y.Id)).ToList();
        await _telegramAppsProvider.BulkAddOrUpdateAsync(toUpdate);
    }

    public async Task<List<TelegramAppDto>> GetTelegramAppAsync(QueryTelegramAppsInput input)
    {
        if (input == null ||
            (input.Names.IsNullOrEmpty() && input.Aliases.IsNullOrEmpty() && input.Ids.IsNullOrEmpty()))
        {
            return new List<TelegramAppDto>();
        }

        try
        {
            var (count, telegramAppindices) = await _telegramAppsProvider.GetTelegramAppsAsync(input);
            if (telegramAppindices.IsNullOrEmpty())
            {
                return new List<TelegramAppDto>();
            }

            return _objectMapper.Map<List<TelegramAppIndex>, List<TelegramAppDto>>(telegramAppindices);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTelegramAppAsync error. {0}", JsonConvert.SerializeObject(input));
            throw new UserFriendlyException($"System exception occurred during querying telegram apps. {e.Message}");
        }
    }

    public async Task<IDictionary<string, TelegramAppDetailDto>> SaveTelegramAppDetailAsync(IDictionary<string, TelegramAppDetailDto> telegramAppDetailDtos)
    {
        if (telegramAppDetailDtos.IsNullOrEmpty())
        {
            return telegramAppDetailDtos;
        }

        var telegramAppDtos = await GetTelegramAppAsync(new QueryTelegramAppsInput
        {
            Names = telegramAppDetailDtos.Keys.ToList(),
        });
        if (telegramAppDtos.IsNullOrEmpty())
        {
            return new Dictionary<string, TelegramAppDetailDto>();
        }

        var res = new Dictionary<string, TelegramAppDetailDto>();
        foreach (var telegramAppDto in telegramAppDtos)
        {
            if (!telegramAppDetailDtos.ContainsKey(telegramAppDto.Title))
            {
                continue;
            }

            var telegramAppDetailDto = telegramAppDetailDtos[telegramAppDto.Title];
            var detailData = telegramAppDetailDto.Data?.FirstOrDefault();
            var url = detailData?.Attributes?.Url;
            var longDescription = detailData?.Attributes?.Long_description;
            var screenshots = detailData?.Attributes?.Screenshots?.Data ??
                              new List<TelegramAppScreenshotsItem>();
            var screenshotList = screenshots.Select(item => item?.Attributes?.Url).ToList();
            telegramAppDto.Url = url;
            telegramAppDto.LongDescription = longDescription;
            telegramAppDto.Screenshots = screenshotList;
            res[telegramAppDto.Title] = telegramAppDetailDto;
        }

        await SaveTelegramAppsAsync(telegramAppDtos);

        return res;
    }
    
    private async Task CheckAddress(string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }
    }
}