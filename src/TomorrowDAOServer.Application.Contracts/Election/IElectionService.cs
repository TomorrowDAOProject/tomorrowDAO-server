using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Election.Dto;

namespace TomorrowDAOServer.Election;

public interface IElectionService
{
    Task<List<string>> GetHighCouncilMembersAsync(HighCouncilMembersInput input);
}