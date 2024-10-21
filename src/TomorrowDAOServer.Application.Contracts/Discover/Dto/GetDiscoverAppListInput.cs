using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Discover.Dto;

public class GetDiscoverAppListInput
{
    [Required] public string ChainId { get; set; }
    [Required] public string Category { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 8;
    public List<string> Aliases { get; set; } = new();
}