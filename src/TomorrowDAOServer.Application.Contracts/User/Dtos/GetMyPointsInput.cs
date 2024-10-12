using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.User.Dtos;

public class GetMyPointsInput
{
    [Required] public string ChainId { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 10;
}