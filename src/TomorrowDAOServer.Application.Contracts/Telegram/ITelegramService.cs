using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Telegram;

public interface ITelegramService
{
    Task SaveTelegramAppAsync(TelegramAppDto telegramAppDto, string chainId);

    Task SaveTelegramAppsAsync(List<TelegramAppDto> telegramAppDtos);
    Task<List<TelegramAppDto>> GetTelegramAppAsync(QueryTelegramAppsInput input);

    Task<IDictionary<string, TelegramAppDetailDto>> SaveTelegramAppDetailAsync(LoadTelegramAppsDetailInput input,
        IDictionary<string, TelegramAppDetailDto> telegramAppDetailDtos);
}