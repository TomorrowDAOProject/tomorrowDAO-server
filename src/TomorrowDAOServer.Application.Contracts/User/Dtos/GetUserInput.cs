using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.User.Dtos;

public class GetUserInput : PagedResultRequestDto
{
    public string ChainId { get; set; }
}