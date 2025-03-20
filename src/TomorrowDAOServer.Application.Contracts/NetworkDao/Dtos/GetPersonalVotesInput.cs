using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class GetPersonalVotesInput
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string Voter { get; set; }
    [Required]
    public string ProposalId { get; set; }
}