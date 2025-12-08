using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.User.Dtos;

public class GetAllUserPointsInput : PagedResultRequestDto
{
    [Required] public string ChainId { get; set; }
}

public class UserPointsDto
{
    public string UserId { get; set; }
    public string Address { get; set; }
    public long Points { get; set; }
}