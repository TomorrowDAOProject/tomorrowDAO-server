using System.Threading.Tasks;
using TomorrowDAOServer.NetworkDao.Dtos;

namespace TomorrowDAOServer.NetworkDao;

public interface INetworkDaoOrgService
{
    Task<GetOrgOfOwnerListPagedResult> GetOrgOfOwnerListAsync(GetOrgOfOwnerListInput input);
}