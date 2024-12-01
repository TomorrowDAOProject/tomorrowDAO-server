using System.Threading.Tasks;
using TomorrowDAOServer.Open.Dto;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.Application.Services;

namespace TomorrowDAOServer.Open;

public class OpenService : ApplicationService, IOpenService
{
    private readonly IVoteProvider _voteProvider;

    public OpenService(IVoteProvider voteProvider)
    {
        _voteProvider = voteProvider;
    }

    public async Task<TaskStatusResponse> GetTaskStatusAsync(string address, string proposalId)
    {
        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(proposalId))
        {
            return new TaskStatusResponse { Result = false}; 
        }
        var count = await _voteProvider.CountByVoterAndVotingItemIdAsync(address, proposalId);
        return new TaskStatusResponse { Result = count > 0 };
    }
}