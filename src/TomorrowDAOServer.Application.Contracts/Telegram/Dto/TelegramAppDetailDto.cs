using System.Collections.Generic;

namespace TomorrowDAOServer.Telegram.Dto;

public class TelegramAppDetailDto
{
    public List<TelegramAppDetailData> Data { get; set; }
    public TelegramAppDetailMeta Meta { get; set; }
}

public class TelegramAppDetailData
{
    public long Id { get; set; }
    public TelegramAppDetailDataAttr Attributes { get; set; }
}

public class TelegramAppDetailDataAttr
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string Path { get; set; }
    public string CreatedAt { get; set; }
    public string UpdatedAt { get; set; }
    public string PublishedAt { get; set; }
    public string Locale { get; set; }
    public string EditorsChoice { get; set; }
    public string WebappUrl { get; set; }
    public string CommunityUrl { get; set; }
    public string Long_description { get; set; }
    public string StartParam { get; set; }
    public string Ecosystem { get; set; }
    public bool Ios { get; set; }
    public string AnalyticsId { get; set; }
    // public string Icon { get; set; }
    // public string Categories { get; set; }
    // public string Poster { get; set; }
    public TelegramAppScreenshots Screenshots { get; set; }
}

public class TelegramAppScreenshots
{
    public List<TelegramAppScreenshotsItem> Data { get; set; }
}

public class TelegramAppScreenshotsItem
{
    public string Id { get; set; }
    public TelegramAppImageAttributes Attributes { get; set; }
}

public class TelegramAppImageAttributes
{
    public string Name { get; set; }
    public string AlternativeText { get; set; }
    public string Caption { get; set; }
    public long Width { get; set; }
    public long Height { get; set; }
    public string Hash { get; set; }
    public string Ext { get; set; }
    public string Mime { get; set; }
    public double Size { get; set; }
    public string Url { get; set; }
    public string PreviewUrl { get; set; }
    public string Provider { get; set; }
    public string ProviderMetadata { get; set; }
    public string CreatedAt { get; set; }
    public string UpdatedAt { get; set; }
}

public class TelegramAppDetailMeta
{
    public TelegramAppDetailMetaPagination Pagination { get; set; }
}

public class TelegramAppDetailMetaPagination
{
    public int Start { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
}

public class LoadTelegramAppsDetailInput
{
    public string ChainId { get; set; }

    public string Url { get; set; }
    
    public Dictionary<string, string> Header { get; set; }

    //<name, alias>
    public Dictionary<string, string> Apps { get; set; }
}