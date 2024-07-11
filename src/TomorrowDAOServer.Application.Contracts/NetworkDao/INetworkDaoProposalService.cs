using System.Threading.Tasks;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.NetworkDao.Dto;

namespace TomorrowDAOServer.NetworkDao;

public interface INetworkDaoProposalService
{
    
    Task<HomePageResponse> GetHomePageAsync(HomePageRequest proposalResult);

    Task<ExplorerProposalResponse> GetProposalListAsync(ProposalListRequest request);

    Task<NetworkDaoProposalDto> GetProposalInfoAsync(ProposalInfoRequest request);

}