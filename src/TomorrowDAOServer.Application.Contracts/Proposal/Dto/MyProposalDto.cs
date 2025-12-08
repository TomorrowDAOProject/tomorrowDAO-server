using System;
using System.Collections.Generic;

namespace TomorrowDAOServer.Proposal.Dto;

public class MyProposalDto
{
    public string ChainId { get; set; }

    public string Symbol { get; set; }
    public string Decimal{ get; set; }
    
    public long AvailableUnStakeAmount { get; set; }

    public long StakeAmount { get; set; }
    
    public long VotesAmountTokenBallot { get; set; }
    
    public long VotesAmountUniqueVote { get; set; }

    public bool CanVote { get; set; }
    public List<WithdrawDto> WithdrawList { get; set; } = new();
}

public class WithdrawDto
{
    public List<string> ProposalIdList { get; set; } = new();
    public long WithdrawAmount { get; set; }
}