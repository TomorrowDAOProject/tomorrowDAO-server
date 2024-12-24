using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class TelegramAppIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Alias { get; set; }
    [Keyword] public string Title { get; set; }
    [Keyword] public string Icon { get; set; }
    [Keyword] public string Description { get; set; }
    public bool EditorChoice { get; set; }
    [Keyword] public string Url { get; set; }
    [Keyword] public string LongDescription { get; set; }
    public List<string> Screenshots { get; set; }
    public List<TelegramAppCategory> Categories { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public SourceType SourceType { get; set; } = SourceType.Telegram;
    [Keyword] public string Creator { get; set; }
    public DateTime LoadTime { get; set; }
    [Keyword] public string BackIcon { get; set; }
    public List<string> BackScreenshots { get; set; }
    public long TotalPoints { get; set; }
    public long TotalVotes { get; set; }
    public long TotalLikes { get; set; }
    public long TotalOpenTimes { get; set; }
}