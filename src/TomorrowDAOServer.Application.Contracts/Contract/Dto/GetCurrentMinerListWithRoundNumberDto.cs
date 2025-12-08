using System.Collections.Generic;

namespace TomorrowDAOServer.Contract.Dto;

public class GetCurrentMinerListWithRoundNumberDto
{
    public List<string> Pubkeys { get; set; }
    
    public long RoundNumber { get; set; }
}