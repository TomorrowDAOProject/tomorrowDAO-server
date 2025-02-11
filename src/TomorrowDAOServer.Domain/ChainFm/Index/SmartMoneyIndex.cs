using System;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;

namespace TomorrowDAOServer.ChainFm.Index;

public class SmartMoneyIndex : AbstractEntity<string>, IIndexBuild
{
    public virtual string Id { get; set; }
    public string Address { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public SmartMoneySourceEnum Source { get; set; }
}