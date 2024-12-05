using System.Text;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Serilog;
using TomorrowDAOServer.Auth.Dtos;
using TomorrowDAOServer.Auth.Options;
using TomorrowDAOServer.Auth.Verifier.Constants;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using AElf.Client;
using AElf.Client.Dto;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Google.Protobuf;
using Portkey.Contracts.CA;
using TomorrowDAOServer.Auth.Common;
using TomorrowDAOServer.Auth.Portkey.Providers;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Auth.Verifier.Providers;

public class WalletUserVerifierProvider : IVerifierProvider
{
    private ILogger<WalletUserVerifierProvider> _logger;
    //private IAbpDistributedLock _distributedLock;
    private IOptionsMonitor<ContractOptions> _contractOptions;
    //private IClusterClient _clusterClient;
    private IOptionsMonitor<GraphQlOption> _graphQlOptions;
    private IOptionsMonitor<ChainOptions> _chainOptions;
    private IPortkeyProvider _portkeyProvider;

    public WalletUserVerifierProvider(ILogger<WalletUserVerifierProvider> logger,
        IOptionsMonitor<GraphQlOption> graphQlOptions, IOptionsMonitor<ChainOptions> chainOptions,
        IOptionsMonitor<ContractOptions> contractOptions, IPortkeyProvider portkeyProvider)
    {
        _logger = logger;
        _graphQlOptions = graphQlOptions;
        _chainOptions = chainOptions;
        _contractOptions = contractOptions;
        _portkeyProvider = portkeyProvider;
    }

    public string GetLoginType()
    {
        return LoginType.LoginType_Portkey;
    }

    public async Task<VerifierResultDto> VerifyUserInfoAsync(ExtensionGrantContext context)
    {
        // _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SignatureGrantHandler>>();
        // _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();
        // _clusterClient = context.HttpContext.RequestServices.GetRequiredService<IClusterClient>();
        // _graphQlOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<GraphQlOption>>();
        // _chainOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<ChainOptions>>();
        // _contractOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<ContractOptions>>();
        // _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();
        // _graphQlOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<GraphQlOption>>();
        // _chainOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<ChainOptions>>();
        
        var publicKeyVal = context.Request.GetParameter("publickey").ToString();
        var signatureVal = context.Request.GetParameter("signature").ToString();
        var chainId = context.Request.GetParameter("chain_id").ToString();
        var caHash = context.Request.GetParameter("ca_hash").ToString();
        var timestampVal = context.Request.GetParameter("timestamp").ToString();
        var address = context.Request.GetParameter("address").ToString();
        var source = context.Request.GetParameter("source").ToString();

        var invalidParamResult = CheckParams(publicKeyVal, signatureVal, chainId, address, timestampVal);
        if (invalidParamResult != null)
        {
            return new VerifierResultDto
            {
                IsVerified = false,
                ForbidResult = invalidParamResult
            };
        }

        var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        var signature = ByteArrayHelper.HexStringToByteArray(signatureVal);
        var signAddress = Address.FromPublicKey(publicKey).ToBase58();

        var timestamp = long.Parse(timestampVal!);
        var time = DateTime.UnixEpoch.AddMilliseconds(timestamp);
        var timeRangeConfig = context.HttpContext.RequestServices
            .GetRequiredService<IOptionsSnapshot<TimeRangeOption>>()
            .Value;

        if (time < DateTime.UtcNow.AddMinutes(-timeRangeConfig.TimeRange) ||
            time > DateTime.UtcNow.AddMinutes(timeRangeConfig.TimeRange))
        {
            return new VerifierResultDto
            {
                IsVerified = false,
                ForbidResult = ForbidResultHelper.GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    $"The time should be {timeRangeConfig.TimeRange} minutes before and after the current time.")
            };
        }

        var newSignText = """
                          Welcome to TMRWDAO! Click to sign in to the TMRWDAO platform! This request will not trigger any blockchain transaction or cost any gas fees.

                          signature: 
                          """+string.Join("-", address, timestampVal);
        Log.Information("newSignText:{newSignText}",newSignText);
        if (!CryptoHelper.RecoverPublicKey(signature,
                HashHelper.ComputeFrom(Encoding.UTF8.GetBytes(newSignText).ToHex()).ToByteArray(),
                out var managerPublicKey))
        {
            return new VerifierResultDto
            {
                IsVerified = false,
                ForbidResult = ForbidResultHelper.GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    "Signature validation failed new.")
            };
        }

        if (!CryptoHelper.RecoverPublicKey(signature,
                HashHelper.ComputeFrom(string.Join("-", address, timestampVal)).ToByteArray(),
                out var managerPublicKeyOld))
        {
            return new VerifierResultDto
            {
                IsVerified = false,
                ForbidResult = ForbidResultHelper.GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Signature validation failed old.")
            };
        }

        if (!(managerPublicKey.ToHex() == publicKeyVal || managerPublicKeyOld.ToHex() == publicKeyVal))
        {
            return new VerifierResultDto
            {
                IsVerified = false,
                ForbidResult = ForbidResultHelper.GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Invalid publicKey or signature.")
            };
        }

        Log.Information(
            "publicKeyVal:{0}, signatureVal:{1}, address:{2}, caHash:{3}, chainId:{4}, timestamp:{5}",
            publicKeyVal, signatureVal, address, caHash, chainId, timestamp);

        var guardianIdentifier = string.Empty;
        List<AddressInfo> addressInfos;
        if (!string.IsNullOrWhiteSpace(caHash))
        {
            var managerCheck = await CheckAddressAsync(chainId, _graphQlOptions.CurrentValue.Url, caHash, signAddress,
                _chainOptions.CurrentValue);
            if (!managerCheck.HasValue || !managerCheck.Value)
            {
                _logger.LogError("Manager validation failed. caHash:{0}, address:{1}, chainId:{2}",
                    caHash, address, chainId);
                return new VerifierResultDto
                {
                    IsVerified = false,
                    ForbidResult = ForbidResultHelper.GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Manager validation failed.")
                };
            }
            addressInfos = await GetAddressInfosAsync(caHash);

            var guardianResultDto = await _portkeyProvider.GetGuardianIdentifierAsync(caHash, string.Empty);
            var guardianDto = await _portkeyProvider.GetLoginGuardianAsync(guardianResultDto);
            if (guardianDto != null)
            {
                guardianIdentifier = guardianDto.GuardianIdentifier;
            }
        }
        else
        {
            if (address != signAddress)
            {
                return new VerifierResultDto
                {
                    IsVerified = false,
                    ForbidResult = ForbidResultHelper.GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Invalid address or pubkey.")
                };
            }

            addressInfos = new List<AddressInfo>
            {
                new()
                {
                    ChainId = chainId,
                    Address = address
                }
            };
        }

        return new VerifierResultDto
        {
            IsVerified = true,
            CaHash = caHash,
            Address = address,
            GuardianIdentifier = guardianIdentifier,
            CreateChainId = chainId,
            AddressInfos = addressInfos,
            ForbidResult = null
        };
    }

    private ForbidResult CheckParams(string publicKeyVal, string signatureVal, string chainId, string address,
        string timestamp)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(publicKeyVal))
        {
            errors.Add("invalid parameter publish_key.");
        }

        if (string.IsNullOrWhiteSpace(signatureVal))
        {
            errors.Add("invalid parameter signature.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            errors.Add("invalid parameter address.");
        }

        if (string.IsNullOrWhiteSpace(chainId))
        {
            errors.Add("invalid parameter chain_id.");
        }

        if (string.IsNullOrWhiteSpace(timestamp))
        {
            errors.Add("invalid parameter timestamp.");
        }

        if (errors.Count > 0)
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = GetErrorMessage(errors)
                }!));
        }

        return null;
    }

    private string GetErrorMessage(List<string> errors)
    {
        var message = string.Empty;

        errors?.ForEach(t => message += $"{t}, ");

        return message.Contains(',') ? message.TrimEnd().TrimEnd(',') : message;
    }

    private async Task<bool?> CheckAddressAsync(string chainId, string graphQlUrl, string caHash, string manager,
        ChainOptions chainOptions)
    {
        var graphQlResult = await CheckAddressFromGraphQlAsync(graphQlUrl, caHash, manager);
        if (!graphQlResult.HasValue || !graphQlResult.Value)
        {
            Log.Debug("graphql is invalid.");
            return await CheckAddressFromContractAsync(chainId, caHash, manager, chainOptions);
        }

        return true;
    }

    private async Task<bool?> CheckAddressFromContractAsync(string chainId, string caHash, string manager,
        ChainOptions chainOptions)
    {
        var param = new GetHolderInfoInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            LoginGuardianIdentifierHash = Hash.Empty
        };

        var output =
            await CallTransactionAsync<GetHolderInfoOutput>(chainId, AuthConstant.GetHolderInfo, param, false,
                chainOptions);

        return output?.ManagerInfos?.Any(t => t.Address.ToBase58() == manager);
    }

    private async Task<bool?> CheckAddressFromGraphQlAsync(string url, string caHash,
        string managerAddress)
    {
        var cHolderInfos = await GetHolderInfosAsync(url, caHash);
        var caHolder = cHolderInfos?.CaHolderInfo?.SelectMany(t => t.ManagerInfos);
        return caHolder?.Any(t => t.Address == managerAddress);
    }

    private async Task<List<AddressInfo>> GetAddressInfosAsync(string caHash)
    {
        var addressInfos = new List<AddressInfo>();
        var holderInfoDto = await GetHolderInfosAsync(_graphQlOptions.CurrentValue.Url, caHash);

        var chainIds = new List<string>();
        if (holderInfoDto != null && !holderInfoDto.CaHolderInfo.IsNullOrEmpty())
        {
            addressInfos.AddRange(holderInfoDto.CaHolderInfo.Select(t => new AddressInfo
                { ChainId = t.ChainId, Address = t.CaAddress }));
            chainIds = holderInfoDto.CaHolderInfo.Select(t => t.ChainId).ToList();
        }

        var chains = _chainOptions.CurrentValue.ChainInfos.Select(key => _chainOptions.CurrentValue.ChainInfos[key.Key])
            .Select(chainOptionsChainInfo => chainOptionsChainInfo.ChainId).Where(t => !chainIds.Contains(t));

        foreach (var chainId in chains)
        {
            var addressInfo = await GetAddressInfoAsync(chainId, caHash);
            if (addressInfo != null)
            {
                addressInfos.Add(addressInfo);
            }
        }

        return addressInfos;
    }

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(SignatureGrantHandlerExceptionHandler),
    //     MethodName = nameof(SignatureGrantHandlerExceptionHandler.HandleGetAddressInfoAsync), Message = "get holder from chain error",
    //     LogTargets = new []{"caHash"})]
    public virtual async Task<AddressInfo> GetAddressInfoAsync(string chainId, string caHash)
    {
        try
        {
            var param = new GetHolderInfoInput
            {
                CaHash = Hash.LoadFromHex(caHash),
                LoginGuardianIdentifierHash = Hash.Empty
            };

            var output = await CallTransactionAsync<GetHolderInfoOutput>(chainId, AuthConstant.GetHolderInfo, param,
                false,
                _chainOptions.CurrentValue);

            return new AddressInfo()
            {
                Address = output.CaAddress.ToBase58(),
                ChainId = chainId
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get holder from chain error. CaHash={0},ChainId={1}", caHash, chainId);
            return null;
        }
    }

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "CallTransaction error",
    //     LogTargets = new []{"chainId", "methodName"}, ReturnDefault = default)]
    public virtual async Task<T> CallTransactionAsync<T>(string chainId, string methodName, IMessage param,
        bool isCrossChain, ChainOptions chainOptions) where T : class, IMessage<T>, new()
    {
        try
        {
            var chainInfo = chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            var address = client.GetAddressFromPrivateKey(_contractOptions.CurrentValue.CommonPrivateKeyForCallTx);

            var contractAddress = isCrossChain
                ? (await client.GetContractAddressByNameAsync(HashHelper.ComputeFrom(ContractName.CrossChain)))
                .ToBase58()
                : chainInfo.ContractAddress;

            var transaction =
                await client.GenerateTransactionAsync(address, contractAddress,
                    methodName, param);

            var txWithSign =
                client.SignTransaction(_contractOptions.CurrentValue.CommonPrivateKeyForCallTx, transaction);
            var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });

            var value = new T();
            value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));
            return value;
        }
        catch (Exception e)
        {
            if (methodName != AuthConstant.GetHolderInfo)
            {
                _logger.LogError(e, "CallTransaction error, chain id:{chainId}, methodName:{methodName}", chainId,
                    methodName);
            }

            _logger.LogError(e, "CallTransaction error, chain id:{chainId}, methodName:{methodName}", chainId,
                methodName);
            return null;
        }
    }

    private async Task<HolderInfoIndexerDto> GetHolderInfosAsync(string url, string caHash)
    {
        using var graphQlClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());
        var request = new GraphQLRequest
        {
            Query = @"
			    query($caHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caHash:$caHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData}}
                }",
            Variables = new
            {
                caHash, skipCount = 0, maxResultCount = 10
            }
        };

        var graphQlResponse = await graphQlClient.SendQueryAsync<HolderInfoIndexerDto>(request);
        return graphQlResponse.Data;
    }
}