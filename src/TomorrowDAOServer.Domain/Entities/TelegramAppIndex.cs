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
    public string Icon { get; set; }
    public string Description { get; set; }
    public bool EditorChoice { get; set; }
    public string Url { get; set; }
    public string LongDescription { get; set; }
    public List<string> Screenshots { get; set; }
    public List<TelegramAppCategory> Categories { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public SourceType SourceType { get; set; } = SourceType.Telegram;
    [Keyword] public string Creator { get; set; }
    public DateTime LoadTime { get; set; }
}