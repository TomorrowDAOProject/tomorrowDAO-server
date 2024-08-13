using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class NetworkDaoOptions
{
    
    public int CurrentTermMiningRewardCacheSeconds { get; set; } = 60;
    public int ProposalVoteCountCacheSeconds { get; set; } = 60;

    public List<string> PopularSymbols { get; set; } = new();
    public List<string> AetherLinkSymbols { get; set; } = new();
    public List<string> AwakenSymbols { get; set; } = new();

}