using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.User.Dtos;

public class ViewAdInput
{
    [Required] public string ChainId { get; set; }
    [Required] public string Signature { get; set; }
    [Required] public long TimeStamp { get; set; }
}