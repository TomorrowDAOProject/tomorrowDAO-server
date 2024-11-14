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
    Task<AddTeamDescResultDto> AddTeamDescriptionAsync(AddTeamDescInput input);
    Task<UpdateTeamStatusResultDto> UpdateTeamStatusAsync(UpdateTeamStatusInput input);
    Task<bool> LoadVoteTeamHistoryDateAsync(LoadVoteTeamDescHistoryInput input);
}