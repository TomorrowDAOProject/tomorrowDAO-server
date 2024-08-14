using System.Collections.Generic;

namespace TomorrowDAOServer.Telegram.Dto;

public class TelegramAppDto
{
    public string Id { get; set; }
    public string Alias { get; set; }
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public bool EditorChoice { get; set; }
}

public class SaveTelegramAppsInput
{
    public string ChainId { get; set; }
    public TelegramAppDto TelegramAppDto { get; set; }
}

public class LoadTelegramAppsInput
{
    public string ChainId { get; set; }
    public string Url { get; set; }
    public ContentType ContentType { get; set; } = ContentType.Body;
}

public class QueryTelegramAppsInput
{
    public List<string> Names { get; set; }
    public List<string> Aliases { get; set; }
    public List<string> Ids { get; set; }
}

public enum ContentType
{
    Body = 1,
    Script = 2
}