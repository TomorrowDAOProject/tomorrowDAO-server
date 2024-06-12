using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.DAO.Dtos;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.DAO;

public interface IDAOAppService
{
    Task<DAOInfoDto> GetDAOByIdAsync(GetDAOInfoInput input);
    Task<PagedResultDto<HcMemberDto>> GetMemberListAsync(GetHcMemberInput input);
    Task<PagedResultDto<DAOListDto>> GetDAOListAsync(QueryDAOListInput request);
    Task<List<string>> GetBPList(string chainId);
    Task<List<MyDAOListDto>> GetMyDAOListAsync(QueryMyDAOListInput input);
}