using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Treasury.Dto;

public class GetTreasuryAddressInput
{
    [Required] public string ChainId { get; set; }
    
    public string DaoId { get; set; }

    public string Alias { get; set; }
}