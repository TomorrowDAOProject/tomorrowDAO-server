using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class LoadContractHistoryInput
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string OperateChainId { get; set; }
    public int PageSize { get; set; } = 3000;
    public int PageNum { get; set; } = 1;
}