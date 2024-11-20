using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.User.Dtos;

public class SaveTgInfoInput
{
    [Required] public string ChainId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string TrackId { get; set; } = string.Empty;
}