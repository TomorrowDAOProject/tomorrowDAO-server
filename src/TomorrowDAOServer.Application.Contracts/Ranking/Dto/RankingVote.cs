using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Ranking.Dto;

public class RankingVoteInput
{
    public string ChainId { get; set; }
    public string RawTransaction { get; set; }
    public string TrackId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public TelegramAppCategory? Category { get; set; }
}

public class GetVoteStatusInput
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string ProposalId { get; set; }
    public TelegramAppCategory? Category { get; set; }
}

public class RankingVoteResponse
{
    public RankingVoteStatusEnum Status { get; set; }
    public string TransactionId { get; set; }
    
    public string ProposalId { get; set; }
    public long UserTotalPoints { get; set; }
}

public class RankingVoteRecord
{
    public string TransactionId { get; set; }
    public string VoteTime { get; set; }
    public RankingVoteStatusEnum Status { get; set; }
    public long TotalPoints { get; set; }
}