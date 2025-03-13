using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.ExceptionHandler;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Contract.Dto;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Contract;

public interface IScriptService
{
    public Task<List<string>> GetCurrentBPAsync(string chainId);
    
    public Task<long> GetCurrentBPRoundAsync(string chainId);
    public Task<List<string>> GetCurrentHCAsync(string chainId, string daoId);
    public Task<GetProposalInfoDto> GetProposalInfoAsync(string chainId, string proposalId);
}

public class ScriptService : IScriptService, ITransientDependency
{
    private readonly ITransactionService _transactionService;
    private readonly List<QueryContractInfo> _queryContractInfos;
    private readonly ILogger<ScriptService> _logger;

    public ScriptService(ITransactionService transactionService, IOptionsSnapshot<QueryContractOption> queryContractOption, 
        ILogger<ScriptService> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
        _queryContractInfos = queryContractOption.Value.QueryContractInfoList?.ToList() ?? new List<QueryContractInfo>();
    }

    public async Task<List<string>> GetCurrentBPAsync(string chainId)
    {
        var queryContractInfo = _queryContractInfos.First(x => x.ChainId == chainId);
        var result = await _transactionService.CallTransactionAsync<GetCurrentMinerPubkeyListDto>(chainId, queryContractInfo.PrivateKey, queryContractInfo.ConsensusContractAddress, ContractConstants.GetCurrentMinerPubkeyList);
        return result?.Pubkeys?.Select(x => Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(x)).ToBase58()).ToList() ?? new List<string>();
    }

    public async Task<long> GetCurrentBPRoundAsync(string chainId)
    {
        var queryContractInfo = _queryContractInfos.First(x => x.ChainId == chainId);
        var result = await _transactionService.CallTransactionAsync<GetCurrentMinerListWithRoundNumberDto>(chainId, queryContractInfo.PrivateKey, queryContractInfo.ConsensusContractAddress, ContractConstants.GetCurrentMinerListWithRoundNumber);
        return result?.RoundNumber ?? 0L;
    }

    public async Task<List<string>> GetCurrentHCAsync(string chainId, string daoId)
    {
        var queryContractInfo = _queryContractInfos.First(x => x.ChainId == chainId);
        var result = await _transactionService.CallTransactionAsync<GetVictoriesDto>(chainId, queryContractInfo.PrivateKey, queryContractInfo.ElectionContractAddress, ContractConstants.GetHighCouncilMembers, Hash.LoadFromHex(daoId));
        return result?.Value ?? new List<string>();
    }
    
    public virtual async Task<GetProposalInfoDto> GetProposalInfoAsync(string chainId, string proposalId)
    {
        try
        {
            var queryContractInfo = _queryContractInfos.First(x => x.ChainId == chainId);
            var result = await _transactionService.CallTransactionAsync<GetProposalInfoDto>(chainId, queryContractInfo.PrivateKey, queryContractInfo.GovernanceContractAddress, ContractConstants.GetProposalInfo, Hash.LoadFromHex(proposalId));
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetProposalInfoAsyncException chainId {chainId} proposalId {proposalId}", chainId, proposalId);
            return null;
        }
    }
}