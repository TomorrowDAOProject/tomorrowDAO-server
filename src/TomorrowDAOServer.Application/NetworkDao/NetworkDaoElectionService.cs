using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Election;
using AElf.ExceptionHandler;
using Google.Protobuf.WellKnownTypes;
using Nito.AsyncEx;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Common.Handler;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.NetworkDao;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class NetworkDaoElectionService : TomorrowDAOServerAppService, INetworkDaoElectionService
{
    private readonly IContractProvider _contractProvider;
    private const long RefreshTime = CommonConstant.TenMinutes;
    private long _lastQueryAmount = 0;
    private long _lastUpdateTime = 0;
    
    public NetworkDaoElectionService(IContractProvider contractProvider)
    {
        _contractProvider = contractProvider;
    }

    public virtual async Task<long> GetBpVotingStakingAmount()
    {
        // 10 minute
        if (DateTime.UtcNow.ToUtcMilliSeconds() - _lastUpdateTime <= RefreshTime)
        {
            return _lastQueryAmount;
        }

        var (lastQueryAmount, lastUpdateTime) = await GetBpVotingStakingAmountAsync(_lastQueryAmount, _lastUpdateTime);
        _lastQueryAmount = lastQueryAmount;
        _lastUpdateTime = lastUpdateTime;
        return lastQueryAmount;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleGetBpVotingStakingAmountAsync), 
        Message = "get BP voting staking amount error")]
    public virtual async Task<Tuple<long, long>> GetBpVotingStakingAmountAsync(long lastQueryAmount, long lastUpdateTime)
    {
        var (_, getVotedCandidatesTransaction) = await _contractProvider.CreateCallTransactionAsync(
            CommonConstant.MainChainId,
            SystemContractName.ElectionContract, CommonConstant.ElectionMethodGetVotedCandidates, new Empty());
        var pubkeyList =
            await _contractProvider.CallTransactionAsync<PubkeyList>(CommonConstant.MainChainId,
                getVotedCandidatesTransaction);

        Log.Information("voted candidates count: {0}", pubkeyList?.Value?.Count ?? 0);
        if (pubkeyList == null || pubkeyList.Value.IsNullOrEmpty())
        {
            return new Tuple<long, long>(lastQueryAmount, lastUpdateTime);
        }

        var tasks = new List<Task<CandidateVote>>();
        foreach (var pubkey in pubkeyList.Value!)
        {
            var input = new StringValue
            {
                Value = pubkey.ToHex()
            };
            var (_, tx) = await _contractProvider.CreateCallTransactionAsync(CommonConstant.MainChainId,
                SystemContractName.ElectionContract, CommonConstant.ElectionMethodGetCandidateVote, input);
            tasks.Add(_contractProvider.CallTransactionAsync<CandidateVote>(CommonConstant.MainChainId, tx));
        }

        await tasks.WhenAll();
        var amount = tasks.Sum(task => task.Result?.ObtainedActiveVotedVotesAmount ?? 0);
        Log.Information("BP staking amount: {0}", amount);
        if (amount > 0)
        {
            lastQueryAmount = amount;
            lastUpdateTime = DateTime.UtcNow.ToUtcMilliSeconds();
        }

        return new Tuple<long, long>(lastQueryAmount, lastUpdateTime);
    }
}