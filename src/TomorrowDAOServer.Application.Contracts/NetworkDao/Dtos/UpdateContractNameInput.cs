using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class UpdateContractNameInput
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string ContractName { get; set; }
    [Required]
    public string Address { get; set; }
    [Required]
    public string ContractAddress { get; set; }
    public string CaHash { get; set; }
}

public class UpdateContractNameResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}