using System;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Proposal.Dto;

public class IndexerVoteHistoryDto
{
    public string ChainId { get; set; }
    public string DAOId { get; set; }
    public DateTime TimeStamp { get; set; }
    public string ProposalId { get; set; }
    public string ProposalTitle { get; set; } = string.Empty;
    public VoteOption MyOption { get; set; }
    public int VoteNum { get; set; }
    public string TransactionId { get; set; }
    public string Executer { get; set; } = string.Empty;
    public double VoteNumAfterDecimals { get; set; }
    public string Decimals { get; set; } = "0";
    public string Symbol { get; set; } = string.Empty;
    public string Voter { get; set; } = string.Empty;
}