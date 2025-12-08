using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Dtos;

public class GetTokenInput
{
    [Required] public string Symbol { get; set; }
    [Required] public string ChainId { get; set; }
}

public class TokenDto
{
    public string Symbol { get; set; }
    public string TotalSupply { get; set; }
    public string Supply { get; set; }
    public string Name { get; set; }
    public int Decimals { get; set; }
    public string ChainId { get; set; }
    public bool IsNFT => Symbol.NotNullOrEmpty() && Symbol.Contains('-');
    public string ImageUrl { get; set; }
}