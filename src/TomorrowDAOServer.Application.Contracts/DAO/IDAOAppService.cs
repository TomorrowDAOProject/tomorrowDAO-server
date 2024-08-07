using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.DAO;

public interface IDAOAppService
{
    Task<DAOInfoDto> GetDAOByIdAsync(GetDAOInfoInput input);
    Task<PageResultDto<MemberDto>> GetMemberListAsync(GetMemberListInput input);
    Task<PagedResultDto<DAOListDto>> GetDAOListAsync(QueryDAOListInput request);
    Task<List<string>> GetBPList(string chainId);
    Task<List<MyDAOListDto>> GetMyDAOListAsync(QueryMyDAOListInput input);
    Task<bool> IsDaoMemberAsync(IsDaoMemberInput input);
}