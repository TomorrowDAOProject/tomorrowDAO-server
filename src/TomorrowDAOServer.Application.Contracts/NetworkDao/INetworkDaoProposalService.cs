using System.Threading.Tasks;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.NetworkDao.Dto;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;

namespace TomorrowDAOServer.NetworkDao;

public interface INetworkDaoProposalService
{
    
    Task<HomePageResponse> GetHomePageAsync(HomePageRequest proposalResult);

    Task<ExplorerProposalResponse> GetProposalListAsync(ProposalListRequest request);

    Task<NetworkDaoProposalDto> GetProposalInfoAsync(ProposalInfoRequest request);
    
    Task<GetProposalListPageResult> GetProposalListAsync(GetProposalListInput request);
    Task<GetProposalInfoResultDto> GetProposalInfoAsync(GetProposalInfoInput request);
    Task<GetAppliedListPagedResult> GetAppliedProposalListAsync(GetAppliedListInput input);
}