using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Spider.Dto;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Spider;

public interface ITelegramAppsSpiderService
{
    Task<List<TelegramAppDto>> LoadTelegramAppsAsync(LoadTelegramAppsInput input);
}