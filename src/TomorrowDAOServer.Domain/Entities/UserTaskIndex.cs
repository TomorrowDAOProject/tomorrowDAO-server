using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Entities;

public class UserTaskIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
    public UserTask UserTask { get; set; }
    public UserTaskDetail UserTaskDetail { get; set; }
    public long Points { get; set; }
    public DateTime CompleteTime { get; set; }
}