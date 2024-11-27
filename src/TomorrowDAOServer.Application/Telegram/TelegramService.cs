using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using Aetherlink.PriceServer.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.Sequence;
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
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly IClusterClient _clusterClient;

    public TelegramService(ILogger<TelegramService> logger, IObjectMapper objectMapper,  IUserProvider userProvider,
        ITelegramAppsProvider telegramAppsProvider, IOptionsMonitor<TelegramOptions> telegramOptions,
        IDaoAliasProvider daoAliasProvider, IUserBalanceProvider userBalanceProvider, IClusterClient clusterClient)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _telegramAppsProvider = telegramAppsProvider;
        _userProvider = userProvider;
        _telegramOptions = telegramOptions;
        _daoAliasProvider = daoAliasProvider;
        _userBalanceProvider = userBalanceProvider;
        _clusterClient = clusterClient;
    }

    public async Task SetCategoryAsync(string chainId)
    {
        await CheckAddress(chainId);
        var types = _telegramOptions.CurrentValue.Types;
        if (types.IsNullOrEmpty())
        {
            var allCategories = Enum.GetValues(typeof(TelegramAppCategory)).Cast<TelegramAppCategory>().ToList();
            var random = new Random();

            var all = await _telegramAppsProvider.GetNeedSetCategoryAsync();

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
                .Where(cat => Enum.TryParse(cat, out TelegramAppCategory _))
                .Select(cat => (TelegramAppCategory)Enum.Parse(typeof(TelegramAppCategory), cat))
                .ToList();
            if (categories == null || categories.IsNullOrEmpty())
            {
                continue;
            }
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

    public async Task<List<string>> SaveTelegramAppAsync(BatchSaveAppsInput input)
    {
        var chainId = input.ChainId;
        var address = await CheckUserPermission(chainId);
        
        var telegramApps = input.Apps.Where(t => t.SourceType == SourceType.Telegram).ToList();
        if (!telegramApps.IsNullOrEmpty() && !_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }
        
        _logger.LogInformation("SaveTelegramAppAsync, telegramApp={0}", JsonConvert.SerializeObject(telegramApps));
        var dictionary = await GetLocalTelegramAppIndicesAsync(telegramApps);
        _logger.LogInformation("SaveTelegramAppAsync, IdCount={0}", input.Apps.Count - dictionary.Count);
        var sequenceList = await GetSequenceAsync(input.Apps.Count - dictionary.Count);
        var aliases = new List<string>();
        var telegramAppIndices = _objectMapper.Map<List<SaveTelegramAppsInput>, List<TelegramAppIndex>>(input.Apps);
        foreach (var telegramAppIndex in telegramAppIndices)
        {
            if (telegramAppIndex.SourceType == SourceType.Telegram 
                && dictionary.ContainsKey(telegramAppIndex.Title))
            {
                telegramAppIndex.Id = dictionary[telegramAppIndex.Title].Id;
                telegramAppIndex.Alias = dictionary[telegramAppIndex.Title].Alias;
                telegramAppIndex.CreateTime = dictionary[telegramAppIndex.Title].CreateTime;
                telegramAppIndex.UpdateTime = DateTime.UtcNow;
                telegramAppIndex.Creator = address;
            }
            else
            {
                if (sequenceList.IsNullOrEmpty())
                {
                    throw new UserFriendlyException("Failed to create the APP alias");
                }
                var alias = sequenceList[0];
                sequenceList.RemoveAt(0);
                telegramAppIndex.Id = HashHelper.ComputeFrom(IdGeneratorHelper.GenerateId(telegramAppIndex.SourceType, telegramAppIndex.Title, alias)).ToHex();
                telegramAppIndex.Alias = alias;
                telegramAppIndex.CreateTime = telegramAppIndex.UpdateTime = DateTime.UtcNow;
                telegramAppIndex.Creator = address;
            }
            aliases.Add(telegramAppIndex.Alias);
        }
        await _telegramAppsProvider.BulkAddOrUpdateAsync(telegramAppIndices);
        return aliases;
    }

    private async Task<Dictionary<string, TelegramAppIndex>> GetLocalTelegramAppIndicesAsync(List<SaveTelegramAppsInput> telegramApps)
    {
        if (telegramApps.IsNullOrEmpty())
        {
            return new Dictionary<string, TelegramAppIndex>();
        }
        var titleList = telegramApps.Select(t => t.Title).ToList();
        var (count, list) = await _telegramAppsProvider.GetTelegramAppsAsync(new
            QueryTelegramAppsInput
            {
                Names = titleList,
                SourceType = SourceType.Telegram
            });
        return list?.DistinctBy(t => t.Title).ToDictionary(t => t.Title) ?? new Dictionary<string, TelegramAppIndex>();
    }

    private async Task<List<string>> GetSequenceAsync(int count)
    {
        var sequenceGrain = _clusterClient.GetGrain<ISequenceGrain>(CommonConstant.GrainIdTelegramAppSequence);
        return await sequenceGrain.GetNextValAsync(count);
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
        var exists = await _telegramAppsProvider.GetAllTelegramAppsAsync(new QueryTelegramAppsInput
        {
            Aliases = aliases
            //TODO After initialization, this condition needs to be opened
            //,SourceType = SourceType.Telegram
        });

        var existAppDictionary = exists.GroupBy(t => t.Title)
            .ToDictionary(g => g.Key, g => g.First());
        var now = DateTime.UtcNow;
        foreach (var telegramAppIndex in telegramAppIndices)
        {
            var existApp = existAppDictionary.GetValueOrDefault(telegramAppIndex.Title, new TelegramAppIndex());
            telegramAppIndex.LoadTime = existApp.LoadTime != default && existApp.LoadTime != null ? existApp.LoadTime : now;
            telegramAppIndex.CreateTime = existApp.CreateTime != default && existApp.CreateTime != null ? existApp.CreateTime : now;
            telegramAppIndex.UpdateTime = existApp.UpdateTime != default && existApp.UpdateTime != null ? existApp.UpdateTime : now;
            telegramAppIndex.Categories = existApp.Categories;
            if (SourceType.Telegram == telegramAppIndex.SourceType)
            {
                telegramAppIndex.Url = existApp.Url;
                telegramAppIndex.LongDescription = existApp.LongDescription;
                telegramAppIndex.Screenshots = existApp.Screenshots;
                if (existApp.SourceType == SourceType.FindMini)
                {
                    telegramAppIndex.SourceType = SourceType.FindMini;
                }
                
            }
        }
        
        await _telegramAppsProvider.BulkAddOrUpdateAsync(telegramAppIndices);
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
            SourceType = SourceType.Telegram
        });
        if (telegramAppDtos.IsNullOrEmpty())
        {
            return new Dictionary<string, TelegramAppDetailDto>();
        }

        var res = new Dictionary<string, TelegramAppDetailDto>();
        foreach (var telegramAppDto in telegramAppDtos)
        {
            if (!telegramAppDetailDtos.TryGetValue(telegramAppDto.Title, out var telegramAppDetailDto))
            {
                continue;
            }

            var detailData = telegramAppDetailDto.Data?.FirstOrDefault();
            var url = detailData?.Attributes?.Url;
            var longDescription = detailData?.Attributes?.Long_description;
            var screenshots = detailData?.Attributes?.Screenshots?.Data ??
                              new List<TelegramAppScreenshotsItem>();
            var screenshotList = screenshots.Select(item => item?.Attributes?.Url).ToList();
            telegramAppDto.Url = url;
            telegramAppDto.LongDescription = longDescription;
            telegramAppDto.Screenshots = screenshotList;
            telegramAppDto.CreateTime = DateTime.TryParse(detailData?.Attributes?.CreatedAt, out var createdAt) ? createdAt : telegramAppDto.CreateTime;
            telegramAppDto.UpdateTime = DateTime.TryParse(detailData?.Attributes?.UpdatedAt, out var updatedAt) ? updatedAt : telegramAppDto.UpdateTime;
            res[telegramAppDto.Title] = telegramAppDetailDto;
        }

        await SaveTelegramAppsAsync(telegramAppDtos);

        return res;
    }

    public async Task<PageResultDto<AppDetailDto>> SearchAppAsync(string title)
    {
        var list = await _telegramAppsProvider.SearchAppAsync(title);
        return new PageResultDto<AppDetailDto>
        {
            TotalCount = list.Count, Data = _objectMapper.Map<List<TelegramAppIndex>, List<AppDetailDto>>(list)
        };
    }

    public async Task<bool> AddAppAsync(AddAppInput input)
    {
        var address = await CheckAddress(input.ChainId);
        var title = input.Title;
        await _telegramAppsProvider.AddOrUpdateAsync(new TelegramAppIndex
        {
            Id = HashHelper.ComputeFrom(title).ToHex(),
            Alias = await _daoAliasProvider.GenerateDaoAliasAsync(title), Title = title, Icon = input.Icon, 
            Description = input.Description, EditorChoice = false, Url = input.Url, LongDescription = input.LongDescription,
            Screenshots = input.Screenshots, Categories = input.Categories, CreateTime = DateTime.UtcNow, UpdateTime = DateTime.UtcNow,
            SourceType = SourceType.Telegram, Creator = address, LoadTime = DateTime.UtcNow
        });
        return true;
    }

    private async Task<string> CheckAddress(string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }

        return address;
    }
    
    private async Task<string> CheckUserPermission(string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);

        var symbol = CommonConstant.GetVotigramSymbol(chainId);
        var userBalance = await _userBalanceProvider.GetByIdAsync(GuidHelper.GenerateGrainId(address, chainId, symbol));

        if (_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address) || userBalance is { Amount: >= 1 })
        {
            return address;
        }
        
        throw new UserFriendlyException("Nft Not enough.");
    }
}