using AElf.ExceptionHandler;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.Auth;

public class SignatureGrantHandlerExceptionHandler
{
    public static async Task<FlowBehavior> HandleExceptionAsync(Exception e)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = GetForbidResult(OpenIddictConstants.Errors.ServerError, "Internal error.")
        };
    }

    public static async Task<FlowBehavior> HandleGetAddressInfoAsync(Exception e, string chainId, string caHash)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }

    public static ForbidResult GetForbidResult(string errorType, string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
            }!));
    }
}