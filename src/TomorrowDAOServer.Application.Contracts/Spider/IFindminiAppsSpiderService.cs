using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Spider;

public interface IFindminiAppsSpiderService
{
    Task<List<TelegramAppDto>> LoadAsync(string url);
}