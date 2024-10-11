using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Election;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.NetworkDao;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class NetworkDaoElectionService : INetworkDaoElectionService, ISingletonDependency
{
    private readonly ILogger<NetworkDaoElectionService> _logger;
    private readonly IContractProvider _contractProvider;
    private const long RefreshTime = CommonConstant.TenMinutes;
    private long _lastQueryAmount = 0;
    private long _lastUpdateTime = 0;
    
    public NetworkDaoElectionService(ILogger<NetworkDaoElectionService> logger, IContractProvider contractProvider)
    {
        _logger = logger;
        _contractProvider = contractProvider;
    }

    public async Task<long> GetBpVotingStakingAmount()
    {
        // 10 minute
        if (DateTime.UtcNow.ToUtcMilliSeconds() - _lastUpdateTime <= RefreshTime)
        {
            return _lastQueryAmount;
        }

        try
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
                return _lastQueryAmount;
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
                _lastQueryAmount = amount;
                _lastUpdateTime = DateTime.UtcNow.ToUtcMilliSeconds();
            }

            return _lastQueryAmount;
        }
        catch (Exception e)
        {
            Log.Error(e, "get BP voting staking amount error.");
            return _lastQueryAmount;
        }
    }
}