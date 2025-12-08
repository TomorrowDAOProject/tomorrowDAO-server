using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Discover.Dto;

public class GetDiscoverAppListInput
{
    [Required] public string ChainId { get; set; }
    public string Category { get; set; } = string.Empty;
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 8;
    public string Search { get; set; } = string.Empty;
}