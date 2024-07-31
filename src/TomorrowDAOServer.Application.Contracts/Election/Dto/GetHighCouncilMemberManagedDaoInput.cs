using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Election.Dto;

public class GetHighCouncilMemberManagedDaoInput : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public string DaoId { get; set; }
    public string MemberAddress { get; set; }
}