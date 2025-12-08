using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Election.Dto;

public class HighCouncilMembersInput
{
    [Required] public string ChainId { get; set; }
    public string DaoId { get; set; }
    
    public string Alias { get; set; }
}