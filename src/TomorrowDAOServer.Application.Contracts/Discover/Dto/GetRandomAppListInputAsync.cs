using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Discover.Dto;

public class GetRandomAppListInputAsync
{
    [Required] public string ChainId { get; set; }
    public string Category { get; set; } = string.Empty;
    public int MaxResultCount { get; set; } = 8;
    public List<string> Aliases { get; set; } = new();
}