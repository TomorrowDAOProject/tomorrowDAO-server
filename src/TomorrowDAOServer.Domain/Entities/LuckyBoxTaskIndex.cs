using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class LuckyBoxTaskIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ProposalId { get; set; }
    [Keyword] public string TrackId { get; set; }
    [Keyword] public string Address { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public UpdateTaskStatus UpdateTaskStatus { get; set; }
    public DateTime CompleteTime { get; set; }
}