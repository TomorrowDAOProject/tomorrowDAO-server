using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Open.Dto;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp.Application.Services;

namespace TomorrowDAOServer.Open;

public class OpenService : ApplicationService, IOpenService
{
    private readonly IVoteProvider _voteProvider;
    private readonly IOptionsMonitor<Micro3Options> _micro3Options;
    private readonly ITelegramUserInfoProvider _telegramUserInfoProvider;
    private readonly IOptionsMonitor<FoxCoinOptions> _foxCoinOptions;

    public OpenService(IVoteProvider voteProvider, IOptionsMonitor<Micro3Options> micro3Options, 
        ITelegramUserInfoProvider telegramUserInfoProvider, IOptionsMonitor<FoxCoinOptions> foxCoinOptions)
    {
        _voteProvider = voteProvider;
        _micro3Options = micro3Options;
        _telegramUserInfoProvider = telegramUserInfoProvider;
        _foxCoinOptions = foxCoinOptions;
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

    public async Task<bool> GetFoxCoinTaskStatusAsync(string id, string type)
    {
        if (string.IsNullOrEmpty(id) || type != "foxcoin")
        {
            return false;
        }
        var userInfo = await _telegramUserInfoProvider.GetByTelegramIdAsync(id);
        var address = userInfo?.Address ?? string.Empty;
        var startTime = _foxCoinOptions.CurrentValue.StartTime;
        var count = await _voteProvider.CountByVoterAndTimeAsync(address, startTime);
        return count > 0;
    }

    public async Task<int> GetGalxeTaskStatusAsync(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return 0;
        }

        var proposalId = _micro3Options.CurrentValue.GalxeProposalId;
        if (string.IsNullOrEmpty(proposalId))
        {
            return 0;
        }
        var count = await _voteProvider.CountByVoterAndVotingItemIdAsync(address, proposalId);
        return count > 0 ? 1 : 0;
    }
}