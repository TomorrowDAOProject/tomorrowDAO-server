using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Open.Dto;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.Application.Services;

namespace TomorrowDAOServer.Open;

public class OpenService : ApplicationService, IOpenService
{
    private readonly IVoteProvider _voteProvider;
    private readonly IOptionsMonitor<Micro3Options> _micro3Options;

    public OpenService(IVoteProvider voteProvider, IOptionsMonitor<Micro3Options> micro3Options)
    {
        _voteProvider = voteProvider;
        _micro3Options = micro3Options;
    }

    public async Task<TaskStatusResponse> GetMicro3TaskStatusAsync(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return new TaskStatusResponse { Result = false, Reason = "Invalid address"}; 
        }

        var proposalId = _micro3Options.CurrentValue.ProposalId;
        if (string.IsNullOrEmpty(proposalId))
        {
            return new TaskStatusResponse { Result = false, Reason = "Task not start"}; 
        }
        var count = await _voteProvider.CountByVoterAndVotingItemIdAsync(address, proposalId);
        var result = count > 0;
        return new TaskStatusResponse { Result = result, Reason = !result ? "Not vote in specific poll" : "Completed vote"};
    }
}