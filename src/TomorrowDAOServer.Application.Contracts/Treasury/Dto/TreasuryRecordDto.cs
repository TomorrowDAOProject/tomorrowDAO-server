using System;
using System.Collections.Generic;
using TomorrowDAOServer.Common;
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
    public string Decimals { get; set; }
    public double AmountAfterDecimals { get; set; }
    public string Executor { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public string Memo { get; set; }
    public int TreasuryRecordType { get; set; }
    public DateTime CreateTime { get; set; }
    public string ProposalId { get; set; }
    public string TransactionId { get; set; }
    
    public string OfTransactionId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return string.Empty;
        }

        var result = id.Split(CommonConstant.Middleline);
        return result.Length <= 2 ? string.Empty : result[1];
    }
}

public class GetTreasuryRecordListInput : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public string DaoId { get; set; }
    public string TreasuryAddress { get; set; }
    public string Address { get; set; }
    public List<string> Symbols { get; set; } = new();
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
}