using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Treasury.Dto;

public class GetAllTreasuryAssetsInput 
{
    public string DaoId { get; set; }
    public string ChainId { get; set; }
}