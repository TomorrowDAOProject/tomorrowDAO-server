using System.Collections.Generic;

namespace TomorrowDAOServer.Contract.Dto;

public class GetCurrentMinerPubkeyListDto
{
    public List<string> Pubkeys { get; set; }
}