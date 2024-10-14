using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class DiscoverChoiceIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public TelegramAppCategory TelegramAppCategory { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public DiscoverChoiceType DiscoverChoiceType { get; set; }
    public DateTime UpdateTime { get; set; }
}