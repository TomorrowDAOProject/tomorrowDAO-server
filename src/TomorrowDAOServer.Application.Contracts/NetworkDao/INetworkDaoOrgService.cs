using System.Threading.Tasks;
using TomorrowDAOServer.NetworkDao.Dtos;

namespace TomorrowDAOServer.NetworkDao;

public interface INetworkDaoOrgService
{
    Task<GetOrganizationsPagedResult> GetOrganizationsAsync(GetOrganizationsInput input);
    Task<GetOrgOfOwnerListPagedResult> GetOrgOfOwnerListAsync(GetOrgOfOwnerListInput input);
    Task<GetOrgOfProposerListPagedResult> GetOrgOfProposerListAsync(GetOrgOfProposerListInput input);
}