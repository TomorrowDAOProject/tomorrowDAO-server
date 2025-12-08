using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Dtos.NetworkDao;

public class HomePageRequest
{
    public string ProposalId { get; set; }
    [Required] public string ChainId { get; set; }
    public string Address { get; set; }
}

public class HomePageResponse
{
    public string ChainId { get; set; }
    public ProposalInfo Proposal { get; set; }
    public string TotalVoteNums { get; set; }
    public string VotesOnBP { get; set; }
    public string TreasuryAmount { get; set; }


    public class ProposalInfo
    {
        public string DeployTime { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string VoteTickets { get; set; }
        public string ProposalStatus { get; set; }
    }
}