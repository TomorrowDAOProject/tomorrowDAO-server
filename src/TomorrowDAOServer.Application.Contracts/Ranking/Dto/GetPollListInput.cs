using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Ranking.Dto;

public class GetPollListInput : PagedResultRequestDto
{
    [Required] public string ChainId { get; set; }
    public string Type { get; set; } = CommonConstant.Accumulative;
}