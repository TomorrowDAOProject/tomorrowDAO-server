using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using AElf.ExceptionHandler;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Token.Dto;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;

namespace TomorrowDAOServer.Token;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class IssueTokenService : TomorrowDAOServerAppService, IIssueTokenService
{
    private readonly ILogger<IssueTokenService> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IObjectMapper _objectMapper;

    private readonly JsonSerializerSettings _jsonSerializerSettings =
        JsonSettingsBuilder.New().WithCamelCasePropertyNamesResolver().WithAElfTypesConverters().Build();

    public IssueTokenService(ILogger<IssueTokenService> logger, IContractProvider contractProvider,
        IObjectMapper objectMapper)
    {
        _logger = logger;
        _contractProvider = contractProvider;
        _objectMapper = objectMapper;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultThrowMethodName,
        Message = "IssueTokensAsync error", 
        LogTargets = new []{"input"})]
    public async Task<IssueTokenResponse> IssueTokensAsync(IssueTokensInput input)
    {
        if (input == null || input.ChainId.IsNullOrWhiteSpace() || input.Symbol.IsNullOrWhiteSpace())
        {
            ExceptionHelper.ThrowArgumentException();
        }

        var tokenInfo = await GetTokenInfoAsync(input.ChainId, input.Symbol);
        if (tokenInfo == null || tokenInfo.Symbol.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("Token not found.");
        }

        var tokenResponse = _objectMapper.Map<TokenInfo, IssueTokenResponse>(tokenInfo);
        tokenResponse.TokenOrigin = TokenOriginEnum.TokenContract;
        tokenResponse.ProxyAccountContractAddress =
            _contractProvider.ContractAddress(input.ChainId, CommonConstant.ProxyAccountContractAddressName);
        tokenResponse.TokenContractAddress =
            _contractProvider.ContractAddress(input.ChainId, SystemContractName.TokenContract);
        tokenResponse.RealIssuers = new List<string>() { tokenInfo.Issuer?.ToBase58() };

        var issueChainId = ChainHelper.ConvertChainIdToBase58(tokenInfo.IssueChainId);
        if (issueChainId != input.ChainId)
        {
            //throw new UserFriendlyException("The current chain is not a Token issuance chain");
        }

        var proxyAccount = await GetProxyAccountByProxyAccountAddressAsync(input.ChainId, tokenInfo.Issuer);

        var proxyAccountHash = proxyAccount?.ProxyAccountHash;
        if (proxyAccountHash != null && proxyAccountHash != Hash.Empty)
        {
            tokenResponse.TokenOrigin = TokenOriginEnum.SymbolMarket;
            tokenResponse.ProxyAccountHash = proxyAccountHash.ToHex();
            
            tokenResponse.RealIssuers = new List<string>();
            foreach (var managementAddress in proxyAccount.ManagementAddresses)
            {
                tokenResponse.RealIssuers.Add(managementAddress?.Address?.ToBase58());
            }

            if (input.Amount > 0 && !input.ToAddress.IsNullOrWhiteSpace())
            {
                tokenResponse.ProxyArgs = JsonConvert.SerializeObject(new ForwardCallInput
                {
                    ProxyAccountHash = proxyAccountHash,
                    ContractAddress = Address.FromBase58(tokenResponse.TokenContractAddress),
                    MethodName = CommonConstant.TokenMethodIssue,
                    Args = new IssueInput
                    {
                        Symbol = input.Symbol,
                        Amount = input.Amount,
                        Memo = input.Memo,
                        To = Address.FromBase58(input.ToAddress)
                    }.ToByteString()
                }, _jsonSerializerSettings);
            }
        }

        return tokenResponse;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReThrowMethodName,
        Message = "GetTokenInfoAsync error", 
        LogTargets = new []{"chainId", "symbol"})]
    public virtual async Task<TokenInfo> GetTokenInfoAsync(string chainId, string symbol)
    {
        var (_, transaction) = await _contractProvider.CreateCallTransactionAsync(chainId,
            SystemContractName.TokenContract, CommonConstant.TokenMethodGetTokenInfo,
            new GetTokenInfoInput
            {
                Symbol = symbol
            });
        var tokenInfo =
            await _contractProvider.CallTransactionAsync<TokenInfo>(chainId, transaction);

        return tokenInfo == null ? new TokenInfo() : tokenInfo;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReThrowMethodName,
        Message = "GetProxyAccountByProxyAccountAddressAsync error", 
        LogTargets = new []{"chainId", "proxyAccountAddress"})]
    public virtual async Task<ProxyAccount> GetProxyAccountByProxyAccountAddressAsync(string chainId,
        Address proxyAccountAddress)
    {
        var (_, transaction) = await _contractProvider.CreateCallTransactionAsync(chainId,
            CommonConstant.ProxyAccountContractAddressName,
            CommonConstant.ProxyAccountMethodGetProxyAccountByAddress, proxyAccountAddress);
        var proxyAccount =
            await _contractProvider.CallTransactionAsync<ProxyAccount>(chainId, transaction);

        return proxyAccount == null ? new ProxyAccount() : proxyAccount;
    }
}