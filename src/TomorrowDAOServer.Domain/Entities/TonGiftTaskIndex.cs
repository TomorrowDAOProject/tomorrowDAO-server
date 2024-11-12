using AElf.Indexing.Elasticsearch;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class TonGiftTaskIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public string TaskId { get; set; }
    public string Identifier { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public TonGiftTask TonGiftTask { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public UpdateTaskStatus UpdateTaskStatus { get; set; }
}