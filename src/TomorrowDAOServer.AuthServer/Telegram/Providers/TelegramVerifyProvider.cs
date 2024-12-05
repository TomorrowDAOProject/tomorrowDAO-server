using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Auth.Constants;
using TomorrowDAOServer.Auth.Dtos;
using TomorrowDAOServer.Auth.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Auth.Telegram.Providers;

public class TelegramVerifyProvider : ISingletonDependency, ITelegramVerifyProvider
{
    private ILogger<TelegramVerifyProvider> _logger;
    private readonly IOptionsMonitor<TelegramAuthOptions> _telegramAuthOptions;

    private static readonly ISet<string> FilterKeyNames = new HashSet<string>()
    {
        "address", "chain_id", "client_id", "grant_type", "publickey", "scope", "source", "timestamp", "login_type",
        "hash", "bot_id"
    };

    public TelegramVerifyProvider(ILogger<TelegramVerifyProvider> logger,
        IOptionsMonitor<TelegramAuthOptions> telegramAuthOptions)
    {
        _logger = logger;
        _telegramAuthOptions = telegramAuthOptions;
    }

    public async Task<string> GenerateHashAsync(TelegramAuthDataDto telegramAuthDataDto)
    {
        var dataCheckString = GetDataCheckString(telegramAuthDataDto);
        return GenerateTelegramDataHash.AuthDataHash(_telegramAuthOptions.CurrentValue.BotToken, dataCheckString);
    }

    public async Task<bool> ValidateTelegramDataAsync(IDictionary<string, string> data,
        Func<string, string, string> generateTelegramHash)
    {
        if (data.IsNullOrEmpty() || !data.ContainsKey(CommonConstants.RequestParameterNameHash) ||
            data[CommonConstants.RequestParameterNameHash].IsNullOrWhiteSpace())
        {
            _logger.LogError("telegramData or telegramData[hash] is empty");
            return false;
        }

        var dataCheckString = GetDataCheckString(data);
        var botToken = _telegramAuthOptions.CurrentValue.BotToken;
        var localHash = generateTelegramHash(botToken, dataCheckString);
        if (!localHash.Equals(data[CommonConstants.RequestParameterNameHash]))
        {
            _logger.LogDebug("verification of the telegram information has failed. data={0}",
                JsonConvert.SerializeObject(data));
            return false;
        }

        if (data.ContainsKey(CommonConstants.RequestParameterNameAuthDate) &&
            !data[CommonConstants.RequestParameterNameAuthDate].IsNullOrWhiteSpace())
        {
            var expiredUnixTimestamp = (long)DateTime.UtcNow.AddSeconds(-_telegramAuthOptions.CurrentValue.Expire)
                .Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var authDate = long.Parse(data[CommonConstants.RequestParameterNameAuthDate]);
            if (authDate < expiredUnixTimestamp)
            {
                _logger.LogDebug("verification of the telegram information has failed, login timeout. data={0}",
                    JsonConvert.SerializeObject(data));
                return false;
            }
        }

        return true;
    }

    private static string GetDataCheckString(IDictionary<string, string> data)
    {
        var sortedByKey = data.Keys.OrderBy(k => k);
        var sb = new StringBuilder();
        foreach (var key in sortedByKey)
        {
            if (FilterKeyNames.Contains(key))
            {
                continue;
            }

            sb.AppendLine($"{key}={data[key]}");
        }

        sb.Length -= 1;
        return sb.ToString();
    }

    private static string GetDataCheckString(TelegramAuthDataDto telegramAuthDataDto)
    {
        var keyValuePairs = new Dictionary<string, string>();
        if (!telegramAuthDataDto.Id.IsNullOrWhiteSpace())
        {
            keyValuePairs.Add("id", telegramAuthDataDto.Id);
        }

        if (telegramAuthDataDto.UserName != null)
        {
            keyValuePairs.Add("username", telegramAuthDataDto.UserName);
        }

        if (telegramAuthDataDto.AuthDate != null)
        {
            keyValuePairs.Add("auth_date", telegramAuthDataDto.AuthDate);
        }

        if (telegramAuthDataDto.FirstName != null)
        {
            keyValuePairs.Add("first_name", telegramAuthDataDto.FirstName);
        }

        if (telegramAuthDataDto.LastName != null)
        {
            keyValuePairs.Add("last_name", telegramAuthDataDto.LastName);
        }

        if (telegramAuthDataDto.PhotoUrl != null)
        {
            keyValuePairs.Add("photo_url", telegramAuthDataDto.PhotoUrl);
        }

        return GetDataCheckString(keyValuePairs);
    }
}