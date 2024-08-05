using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Treasury.Dto;

public class GetTreasuryAssetsInput : PagedResultRequestDto
{
    public string DaoId { get; set; }
    
    public string Alias { get; set; }
    public string ChainId { get; set; }
    public ISet<string> Symbols { get; set; }
}