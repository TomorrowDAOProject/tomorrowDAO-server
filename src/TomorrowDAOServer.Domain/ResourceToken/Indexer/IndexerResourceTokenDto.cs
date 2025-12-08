using System;
using System.Collections.Generic;
using TomorrowDAOServer.Entities;

namespace TomorrowDAOServer.ResourceToken.Indexer;

public class IndexerResourceToken
{
    public List<IndexerResourceTokenDto> DataList { get; set; }
}

public class IndexerResourceTokenDto
{
    public string Id { get; set; }
    public string TransactionId { get; set; }
    public string Method { get; set; }
    public string Symbol { get; set; }
    public long ResourceAmount { get; set; }
    public long BaseAmount { get; set; } //elf
    public long FeeAmount { get; set; }
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
    public string TransactionStatus { get; set; }
    public DateTime OperateTime { get; set; }
    public TransactionInfo? TransactionInfo { get; set; }
}