using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Token.Dto;

public class GetUserTokenInput
{
    [Required] public string ChainId { get; set; }
}