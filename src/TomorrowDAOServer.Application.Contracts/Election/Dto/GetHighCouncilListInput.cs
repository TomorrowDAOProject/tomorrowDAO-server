using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Election.Dto;

public class GetHighCouncilListInput : PagedResultRequestDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string DAOId { get; set; }
    public string HighCouncilType { get; set; }
    public long TermNumber { get; set; }
    public string Sorting {get; set; }
}