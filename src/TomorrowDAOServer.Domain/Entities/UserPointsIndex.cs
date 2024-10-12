using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class UserPointsIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
    public Dictionary<string, string> Information { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public UserTask UserTask { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public UserTaskDetail UserTaskDetail { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public PointsType PointsType { get; set; }
    public long Points { get; set; }
    public DateTime PointsTime { get; set; }
}