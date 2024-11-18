using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.GrainDtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;

namespace TomorrowDAOServer.NetworkDao;

public interface INetworkDaoVoteService
{
    Task<GetVotedListPagedResult> GetVotedListAsync(GetVotedListInput input);
    Task<GetAllPersonalVotesPagedResult> GetAllPersonalVotesAsync(GetAllPersonalVotesInput input);
    Task<AddTeamDescResultDto> AddTeamDescriptionAsync(AddTeamDescInput input, bool authRequired = true);
    Task<UpdateTeamStatusResultDto> UpdateTeamStatusAsync(UpdateTeamStatusInput input);
    Task<int> LoadVoteTeamHistoryDateAsync(LoadVoteTeamDescHistoryInput input);
    Task<GetTeamDescResultDto> GetTeamDescAsync(GetTeamDescInput input);
    Task<List<GetTeamDescResultDto>> GetAllTeamDescAsync(GetAllTeamDescInput input);
}