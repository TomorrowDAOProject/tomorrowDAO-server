using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Spider;

public interface ITelegramAppsSpiderService
{
    Task<List<TelegramAppDto>> LoadAllTelegramAppsAsync(LoadAllTelegramAppsInput input);
    Task<List<TelegramAppDto>> LoadTelegramAppsAsync(LoadTelegramAppsInput input);
    Task<IDictionary<string, TelegramAppDetailDto>> LoadTelegramAppsDetailAsync(LoadTelegramAppsDetailInput input);
    Task<IDictionary<string, TelegramAppDetailDto>> LoadAllTelegramAppsDetailAsync(string chainId);
}