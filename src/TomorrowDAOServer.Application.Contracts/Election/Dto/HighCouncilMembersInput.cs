using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Election.Dto;

public class HighCouncilMembersInput
{
    [Required] public string ChainId { get; set; }
    [Required] public string DaoId { get; set; }
}