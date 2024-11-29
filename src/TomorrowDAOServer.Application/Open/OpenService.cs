using System.Threading.Tasks;
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
            return new TaskStatusResponse { Data = new Data { Result = false } }; 
        }
        var count = await _voteProvider.CountByVoterAndVotingItemIdAsync(address, proposalId);
        return new TaskStatusResponse { Data = new Data { Result = count > 0 } };
    }
}