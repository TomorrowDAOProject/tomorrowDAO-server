namespace TomorrowDAOServer.Enums;

public enum ProposalStatus
{
    Empty = 0,
    PendingVote = 1,
    Approved = 2,
    Rejected = 3,
    Abstained = 4,
    BelowThreshold = 5,
    Challenged = 6,
    Vetoed = 7,
    Expired = 8,
    Executed = 9,
    Defeated = 10
}