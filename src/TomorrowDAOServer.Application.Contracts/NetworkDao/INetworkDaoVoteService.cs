using System.Threading.Tasks;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;

namespace TomorrowDAOServer.NetworkDao;

public interface INetworkDaoVoteService
{
    Task<GetVotedListPagedResult> GetVotedListAsync(GetVotedListInput input);
    Task<GetAllPersonalVotesPagedResult> GetAllPersonalVotesAsync(GetAllPersonalVotesInput input);
}