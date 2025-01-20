using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Telegram.Dto;

public class TelegramAppDto : TelegramAppBaseDto
{
    public string BackIcon { get; set; }
    public List<string> BackScreenshots { get; set; }
}

public class TelegramAppDisplayDto : TelegramAppBaseDto
{
    public long TotalComments { get; set; }
    public long TotalOpens { get; set; }
}

public class TelegramAppBaseDto
{
    public string Id { get; set; }
    public string Alias { get; set; }
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public bool EditorChoice { get; set; }
    public string Url { get; set; }
    public string LongDescription { get; set; }
    public List<string> Screenshots { get; set; }
    
    public List<TelegramAppCategory> Categories { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public DateTime LoadTime { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public SourceType SourceType { get; set; }
    public string Creator { get; set; }
    public long TotalPoints { get; set; }
    public long TotalVotes { get; set; }
    public long TotalLikes { get; set; }
}

public class BatchSaveAppsInput
{
    [Required] public string ChainId { get; set; }
    public List<SaveTelegramAppsInput> Apps { get; set; } = new();
}

public class SaveTelegramAppsInput
{
    [Required] public string Title { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string LongDescription { get; set; }
    public List<string> Screenshots { get; set; }
    private List<string> Categories { get; set; } = new();
    public SourceType SourceType { get; set; } = SourceType.Telegram;
}

public class SetCategoryInput
{
    public string ChainId { get; set; }
}

public class LoadAllTelegramAppsInput
{
    public string ChainId { get; set; }
    public ContentType ContentType { get; set; } = ContentType.Body;
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

    public SourceType? SourceType { get; set; } = null;
    public List<SourceType> SourceTypes { get; set; } = new List<SourceType>();
}

public enum ContentType
{
    Body = 1,
    Script = 2
}