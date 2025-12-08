using System.Collections.Generic;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Telegram.Dto;

public class AddAppInput
{
    public string ChainId { get; set; }
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string LongDescription { get; set; }
    public List<string> Screenshots { get; set; }
    public List<TelegramAppCategory> Categories { get; set; }
}