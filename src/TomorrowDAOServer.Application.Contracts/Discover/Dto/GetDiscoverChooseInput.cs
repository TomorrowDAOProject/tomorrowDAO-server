using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Discover.Dto;

public class GetDiscoverChooseInput
{
    [Required] public string ChainId { get; set; }
    public List<string> Choices { get; set; }
}