using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Treasury.Dto;

public class GetTreasuryRecordListResult
{
    public long Item1 { get; set; }
    public List<TreasuryRecordDto> Item2 { get; set; }
}

public class TreasuryRecordDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
    public string DaoId { get; set; }
    public string TreasuryAddress { get; set; }
    public long Amount { get; set; }
    public string Symbol { get; set; }
    public string Executor { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public string Memo { get; set; }
    public int TreasuryRecordType { get; set; }
    public DateTime CreateTime { get; set; }
    public string ProposalId { get; set; }
}

public class GetTreasuryRecordListInput : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public string DaoId { get; set; }
    public string TreasuryAddress { get; set; }
    public string Address { get; set; }
    public List<string> Symbols { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
}