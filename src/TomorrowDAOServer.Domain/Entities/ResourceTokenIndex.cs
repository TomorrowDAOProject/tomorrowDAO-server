using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class ResourceTokenIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string Method { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string Address { get; set; }
    public long ResourceAmount { get; set; }
    public long BaseAmount { get; set; } //elf
    public long FeeAmount { get; set; }
    [Keyword] public string ChainId { get; set; }
    public long BlockHeight { get; set; }
    [Keyword] public string TransactionStatus { get; set; }
    public DateTime OperateTime { get; set; }
    public TransactionInfo? TransactionInfo { get; set; }
}