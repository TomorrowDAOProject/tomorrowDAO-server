using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Telegram;

public interface ITelegramService
{
    Task SetCategoryAsync(string chainId);
    Task SaveTelegramAppAsync(TelegramAppDto telegramAppDto, string chainId);
    Task SaveTelegramAppsAsync(List<TelegramAppDto> telegramAppDtos);
    Task SaveNewTelegramAppsAsync(List<TelegramAppDto> telegramAppDtos);
    Task<List<TelegramAppDto>> GetTelegramAppAsync(QueryTelegramAppsInput input);

    Task<IDictionary<string, TelegramAppDetailDto>> SaveTelegramAppDetailAsync(IDictionary<string, TelegramAppDetailDto> telegramAppDetailDtos);
}