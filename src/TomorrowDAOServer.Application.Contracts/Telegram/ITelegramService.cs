using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Telegram;

public interface ITelegramService
{
    Task SetCategoryAsync(string chainId);
    Task<List<string>> SaveTelegramAppAsync(BatchSaveAppsInput input);
    Task SaveTelegramAppsAsync(List<TelegramAppDto> telegramAppDtos);
    Task SaveNewTelegramAppsAsync(List<TelegramAppDto> telegramAppDtos);
    Task<List<TelegramAppDto>> GetTelegramAppAsync(QueryTelegramAppsInput input);
    Task<IDictionary<string, TelegramAppDetailDto>> SaveTelegramAppDetailAsync(IDictionary<string, TelegramAppDetailDto> telegramAppDetailDtos);
    Task<PageResultDto<AppDetailDto>> SearchAppAsync(string title);
    Task<bool> AddAppAsync(AddAppInput input);
}