using System.Collections.Immutable;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Cryptography;
using AElf.Types;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Orleans;
using TomorrowDAOServer.Auth.Options;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.User.Dtos;
using Google.Protobuf;
using Microsoft.AspNetCore.Identity;
using Portkey.Contracts.CA;
using TomorrowDAOServer.Common;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace TomorrowDAOServer.Auth;

public class SignatureGrantHandler : ITokenExtensionGrant
{
    private ILogger<SignatureGrantHandler> _logger;
    private IAbpDistributedLock _distributedLock;
    private IOptionsMonitor<ContractOptions> _contractOptions;
    private IClusterClient _clusterClient;
    private IOptionsMonitor<GraphQlOption> _graphQlOptions;
    private IOptionsMonitor<ChainOptions> _chainOptions;
    private const string LockKeyPrefix = "TomorrowDAOServer:Auth:SignatureGrantHandler:";

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        try
        {
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
                return invalidParamResult;
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
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    $"The time should be {timeRangeConfig.TimeRange} minutes before and after the current time.");
            }

            var plantText = string.Join("-", address, timestampVal);
            if (!CryptoHelper.RecoverPublicKey(signature, HashHelper.ComputeFrom(plantText).ToByteArray(), out var managerPublicKey))
            {
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Signature validation failed.");
            }

            if (managerPublicKey.ToHex() != publicKeyVal)
            {
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Invalid publicKey or signature.");
            }

            _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SignatureGrantHandler>>();
            _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();
            _clusterClient = context.HttpContext.RequestServices.GetRequiredService<IClusterClient>();
            _graphQlOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<GraphQlOption>>();
            _chainOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<ChainOptions>>();
            _contractOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<ContractOptions>>();
            _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();
            _graphQlOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<GraphQlOption>>();
            _chainOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<ChainOptions>>();

            _logger.LogInformation(
                "publicKeyVal:{0}, signatureVal:{1}, address:{2}, caHash:{3}, chainId:{4}, timestamp:{5}",
                publicKeyVal, signatureVal, address, caHash, chainId, timestamp);

            List<AddressInfo> addressInfos;
            if (!string.IsNullOrWhiteSpace(caHash))
            {
                var managerCheck = await CheckAddressAsync(chainId, _graphQlOptions.CurrentValue.Url, caHash, signAddress,
                    _chainOptions.CurrentValue);
                if (!managerCheck.HasValue || !managerCheck.Value)
                {
                    _logger.LogError("Manager validation failed. caHash:{0}, address:{2}, chainId:{3}",
                        caHash, address, chainId);
                    return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Manager validation failed.");
                }

                addressInfos = await GetAddressInfosAsync(caHash);
            }
            else
            {
                if (address != signAddress)
                {
                    return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Invalid address or pubkey.");
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

            caHash = string.IsNullOrWhiteSpace(caHash) ? string.Empty : caHash;
            var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
            var user = string.IsNullOrWhiteSpace(caHash)
                ? await userManager.FindByNameAsync(address!)
                : await userManager.FindByNameAsync(caHash);

            if (user == null)
            {
                var userId = Guid.NewGuid();
                var createUserResult = await CreateUserAsync(userManager, userId, caHash, address, addressInfos);
                if (!createUserResult)
                {
                    return GetForbidResult(OpenIddictConstants.Errors.ServerError, "Create user failed.");
                }

                user = await userManager.GetByIdAsync(userId);
            }
            else
            {
                var grain = _clusterClient.GetGrain<IUserGrain>(user.Id);
                await grain.CreateUser(new UserGrainDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    CaHash = caHash,
                    AppId = string.IsNullOrEmpty(caHash) ? AuthConstant.NightElfAppId : AuthConstant.PortKeyAppId,
                    AddressInfos = addressInfos,
                });
            }

            var userClaimsPrincipalFactory = context.HttpContext.RequestServices
                .GetRequiredService<IUserClaimsPrincipalFactory<IdentityUser>>();
            var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
            var principal = await signInManager.CreateUserPrincipalAsync(user);
            var claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
            claimsPrincipal.SetScopes("TomorrowDAOServer");
            claimsPrincipal.SetResources(await GetResourcesAsync(context, principal.GetScopes()));
            claimsPrincipal.SetAudiences("TomorrowDAOServer");

            await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimDestinationsManager>()
                .SetAsync(principal);

            return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "generate token error");
            return GetForbidResult(OpenIddictConstants.Errors.ServerError, "Internal error.");
        }
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

    private async Task<bool?> CheckAddressAsync(string chainId, string graphQlUrl, string caHash, string manager,
        ChainOptions chainOptions)
    {
        var graphQlResult = await CheckAddressFromGraphQlAsync(graphQlUrl, caHash, manager);
        if (!graphQlResult.HasValue || !graphQlResult.Value)
        {
            _logger.LogDebug("graphql is invalid.");
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

    private async Task<bool> CreateUserAsync(IdentityUserManager userManager, Guid userId, string caHash,
        string address, List<AddressInfo> addressInfos)
    {
        var result = false;
        await using var handle = await _distributedLock.TryAcquireAsync(name: LockKeyPrefix + caHash);
        if (handle != null)
        {
            var userName = string.IsNullOrEmpty(caHash) ? address : caHash;
            var user = new IdentityUser(userId, userName: userName, email: Guid.NewGuid().ToString("N") + "@tmrwdao.com");
            var identityResult = await userManager.CreateAsync(user);

            if (identityResult.Succeeded)
            {
                _logger.LogInformation("save user info into grain, userId:{userId}", userId.ToString());
                var grain = _clusterClient.GetGrain<IUserGrain>(userId);

                await grain.CreateUser(new UserGrainDto
                {
                    UserId = userId,
                    UserName = userName,
                    CaHash = caHash,
                    AppId = string.IsNullOrEmpty(caHash) ? AuthConstant.NightElfAppId : AuthConstant.PortKeyAppId,
                    AddressInfos = addressInfos,
                });
                _logger.LogInformation("create user success, userId:{userId}", userId.ToString());
            }

            result = identityResult.Succeeded;
        }
        else
        {
            _logger.LogError("do not get lock, keys already exits, userId:{userId}", userId.ToString());
        }

        return result;
    }

    private string GetErrorMessage(List<string> errors)
    {
        var message = string.Empty;

        errors?.ForEach(t => message += $"{t}, ");

        return message.Contains(',') ? message.TrimEnd().TrimEnd(',') : message;
    }

    private ForbidResult GetForbidResult(string errorType, string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
            }!));
    }

    private async Task<IEnumerable<string>> GetResourcesAsync(ExtensionGrantContext context,
        ImmutableArray<string> scopes)
    {
        var resources = new List<string>();
        if (!scopes.Any())
        {
            return resources;
        }

        await foreach (var resource in context.HttpContext.RequestServices.GetRequiredService<IOpenIddictScopeManager>()
                           .ListResourcesAsync(scopes))
        {
            resources.Add(resource);
        }

        return resources;
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
            try
            {
                var addressInfo = await GetAddressInfoAsync(chainId, caHash);
                addressInfos.Add(addressInfo);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "get holder from chain error, caHash:{caHash}", caHash);
            }
        }

        return addressInfos;
    }

    private async Task<AddressInfo> GetAddressInfoAsync(string chainId, string caHash)
    {
        var param = new GetHolderInfoInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            LoginGuardianIdentifierHash = Hash.Empty
        };

        var output = await CallTransactionAsync<GetHolderInfoOutput>(chainId, AuthConstant.GetHolderInfo, param, false,
            _chainOptions.CurrentValue);

        return new AddressInfo()
        {
            Address = output.CaAddress.ToBase58(),
            ChainId = chainId
        };
    }

    private async Task<T> CallTransactionAsync<T>(string chainId, string methodName, IMessage param,
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

            var txWithSign = client.SignTransaction(_contractOptions.CurrentValue.CommonPrivateKeyForCallTx, transaction);
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

    public string Name { get; } = "signature";
}