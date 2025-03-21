using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Discover.Dto;

public class GetVoteAppListInput
{
    [Required] public string ChainId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = CommonConstant.Accumulative;
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 8;
    public string Search { get; set; } = string.Empty;
}