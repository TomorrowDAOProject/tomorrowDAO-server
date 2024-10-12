using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class UserBalanceIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    
    [Keyword] public string Address { get; set; }
    
    public long Amount { get; set; }

    [Keyword] public string Symbol { get; set; }

    public DateTime ChangeTime { get; set; }
    public long BlockHeight { get; set; }
}