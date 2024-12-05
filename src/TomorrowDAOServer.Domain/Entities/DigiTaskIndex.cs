using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class DigiTaskIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string TelegramId { get; set; }
    [Keyword] public string Address { get; set; }
    public long StartTime { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public UpdateTaskStatus UpdateTaskStatus { get; set; }
    public DateTime CompleteTime { get; set; }
}