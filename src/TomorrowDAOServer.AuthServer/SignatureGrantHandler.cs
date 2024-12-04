using System.Collections.Immutable;
using System.Security.Claims;
using System.Text;
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
using TomorrowDAOServer.Auth.Options;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.User.Dtos;
using Google.Protobuf;
using Microsoft.AspNetCore.Identity;
using Portkey.Contracts.CA;
using Serilog;
using TomorrowDAOServer.Auth.Common;
using TomorrowDAOServer.Auth.Verifier;
using TomorrowDAOServer.Auth.Verifier.Constants;
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

    // private IOptionsMonitor<ContractOptions> _contractOptions;
    private IClusterClient _clusterClient;

    // private IOptionsMonitor<GraphQlOption> _graphQlOptions;
    // private IOptionsMonitor<ChainOptions> _chainOptions;
    private IVerifierService _verifierService;
    private const string LockKeyPrefix = "TomorrowDAOServer:Auth:SignatureGrantHandler:";

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(SignatureGrantHandlerExceptionHandler),
    //     MethodName = nameof(SignatureGrantHandlerExceptionHandler.HandleExceptionAsync), Message = "generate token error")]
    public virtual async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SignatureGrantHandler>>();
        _verifierService = context.HttpContext.RequestServices.GetRequiredService<IVerifierService>();
        _clusterClient = context.HttpContext.RequestServices.GetRequiredService<IClusterClient>();
        _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();

        try
        {
            var loginType = context.Request.GetParameter("login_type")?.ToString();
            loginType ??= LoginType.LoginType_Portkey;

            var verifierResultDto = await _verifierService.VerifyUserInfoAsync(loginType, context);
            if (!verifierResultDto.IsVerified)
            {
                return verifierResultDto.ForbidResult;
            }

            var caHash = verifierResultDto.CaHash;
            var address = verifierResultDto.Address;
            var guardianIdentifier = verifierResultDto.GuardianIdentifier;
            var addressInfos = verifierResultDto.AddressInfos;

            caHash = string.IsNullOrWhiteSpace(caHash) ? string.Empty : caHash;
            var userName = GetUserName(caHash, address, guardianIdentifier);
            var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
            var user = await userManager.FindByNameAsync(userName);

            var isNewUser = false;
            if (user == null)
            {
                isNewUser = true;
                var userId = Guid.NewGuid();
                var createUserResult = await CreateUserAsync(userManager, userId, caHash, address, guardianIdentifier,
                    addressInfos);
                if (!createUserResult)
                {
                    return ForbidResultHelper.GetForbidResult(OpenIddictConstants.Errors.ServerError,
                        "Create user failed.");
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
                    GuardianIdentifier = guardianIdentifier,
                    Address = address,
                });
            }

            var userClaimsPrincipalFactory = context.HttpContext.RequestServices
                .GetRequiredService<IUserClaimsPrincipalFactory<IdentityUser>>();
            var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
            var principal = await signInManager.CreateUserPrincipalAsync(user);
            var claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
            var claim = new Claim("new_user", isNewUser ? "1" : "0");
            claim.SetDestinations(new List<string>() { OpenIddictConstants.Destinations.AccessToken }); 
            claimsPrincipal.Identities.First().AddClaim(claim);
            claimsPrincipal.SetScopes("TomorrowDAOServer");
            claimsPrincipal.SetResources(await GetResourcesAsync(context, principal.GetScopes()));
            claimsPrincipal.SetAudiences("TomorrowDAOServer");

            var abpOpenIddictClaimDestinationsManager = context.HttpContext.RequestServices
                .GetRequiredService<AbpOpenIddictClaimsPrincipalManager>();
            await abpOpenIddictClaimDestinationsManager.HandleAsync(context.Request, principal);

            return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "generate token error");
            return ForbidResultHelper.GetForbidResult(OpenIddictConstants.Errors.ServerError, "Internal error.");
        }
    }

    private string GetUserName(string caHash, string address, string guardianIdentifier)
    {
        return string.IsNullOrWhiteSpace(caHash)
            ? string.IsNullOrWhiteSpace(address) ? guardianIdentifier : address
            : caHash;
    }


    private async Task<bool> CreateUserAsync(IdentityUserManager userManager, Guid userId, string caHash,
        string address, string guardianIdentifier, List<AddressInfo> addressInfos)
    {
        var result = false;
        await using var handle = await _distributedLock.TryAcquireAsync(name: LockKeyPrefix + caHash);
        if (handle != null)
        {
            var userName = GetUserName(caHash, address, guardianIdentifier);
            var user = new IdentityUser(userId, userName: userName,
                email: Guid.NewGuid().ToString("N") + "@tmrwdao.com");
            var identityResult = await userManager.CreateAsync(user);

            if (identityResult.Succeeded)
            {
                Log.Information("save user info into grain, userId:{userId}", userId.ToString());
                var grain = _clusterClient.GetGrain<IUserGrain>(userId);

                await grain.CreateUser(new UserGrainDto
                {
                    UserId = userId,
                    UserName = userName,
                    CaHash = caHash,
                    AppId = string.IsNullOrEmpty(caHash)
                        ? AuthConstant.NightElfAppId
                        : AuthConstant.PortKeyAppId,
                    AddressInfos = addressInfos,
                    GuardianIdentifier = guardianIdentifier,
                    Address = address,
                });
                Log.Information("create user success, userId:{userId}", userId.ToString());
            }

            result = identityResult.Succeeded;
        }
        else
        {
            Log.Error("do not get lock, keys already exits, userId:{userId}", userId.ToString());
        }

        return result;
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


    public string Name { get; } = "signature";
}