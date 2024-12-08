using TomorrowDAOServer.Auth.Dtos;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Auth.Telegram.Providers;

public interface ITelegramVerifyProvider
{
    Task<string> GenerateHashAsync(TelegramAuthDataDto telegramAuthDataDto);

    Task<bool> ValidateTelegramDataAsync(IDictionary<string, string> data,
        Func<string, string, string> generateTelegramHash);
}