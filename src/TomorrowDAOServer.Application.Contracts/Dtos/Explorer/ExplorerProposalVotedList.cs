using System;
using System.Collections.Generic;

namespace TomorrowDAOServer.Dtos.Explorer;

public class ExplorerVotedListRequest
{
    public string ProposalId { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

public class ExplorerVoteListResponse
{
    public int Total { get; set; }
    public List<ExplorerVoteDto> List { get; set; }
}

public class ExplorerVoteDto
{
    public string Amount { get; set; }
    public DateTime Time { get; set; }
    public string TxId { get; set; }
    public string Voter { get; set; }
    public string Symbol { get; set; }
    public string Action { get; set; }
}