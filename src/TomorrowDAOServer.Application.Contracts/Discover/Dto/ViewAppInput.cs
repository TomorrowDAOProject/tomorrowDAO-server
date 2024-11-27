using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Discover.Dto;

public class ViewAppInput
{
    [Required] public string ChainId { get; set; }
    [Required] public List<string> Aliases { get; set; } = new();
}