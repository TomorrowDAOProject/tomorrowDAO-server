using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class TonGiftTaskIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string TaskId { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string Identifier { get; set; } // tg id
    [Keyword] public string IdentifierHash { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public TonGiftTask TonGiftTask { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public UpdateTaskStatus UpdateTaskStatus { get; set; }
}