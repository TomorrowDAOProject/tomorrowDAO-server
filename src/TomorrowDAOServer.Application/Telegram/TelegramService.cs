using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Entities;
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

    public TelegramService(ILogger<TelegramService> logger, IObjectMapper objectMapper,
        ITelegramAppsProvider telegramAppsProvider, IUserProvider userProvider,
        IOptionsMonitor<TelegramOptions> telegramOptions)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _telegramAppsProvider = telegramAppsProvider;
        _userProvider = userProvider;
        _telegramOptions = telegramOptions;
    }


    public async Task SaveTelegramAppAsync(TelegramAppDto telegramAppDto, string chainId)
    {
        if (telegramAppDto == null || chainId.IsNullOrWhiteSpace())
        {
            return;
        }

        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.GetId(), chainId);
        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }

        try
        {
            var telegramAppIndex = _objectMapper.Map<TelegramAppDto, TelegramAppIndex>(telegramAppDto);
            await _telegramAppsProvider.SaveTelegramAppAsync(telegramAppIndex);
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
            await _telegramAppsProvider.SaveTelegramAppsAsync(telegramAppIndices);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SaveTelegramAppsAsync error. {0}", JsonConvert.SerializeObject(telegramAppDtos));
            throw new UserFriendlyException($"System exception occurred during saving telegram apps. {e.Message}");
        }
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
}