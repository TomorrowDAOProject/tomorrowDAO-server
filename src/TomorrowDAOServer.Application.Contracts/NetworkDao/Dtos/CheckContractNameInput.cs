using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class CheckContractNameInput
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string ContractName { get; set; }
}

public class CheckContractNameResponse
{
    public bool IsExist { get; set; }
}