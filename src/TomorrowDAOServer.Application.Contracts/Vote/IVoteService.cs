using System.Threading.Tasks;
using TomorrowDAOServer.Vote.Dto;

namespace TomorrowDAOServer.Vote;

public interface IVoteService
{
    Task<VoteSchemeDetailDto> GetVoteSchemeAsync(GetVoteSchemeInput input);
}