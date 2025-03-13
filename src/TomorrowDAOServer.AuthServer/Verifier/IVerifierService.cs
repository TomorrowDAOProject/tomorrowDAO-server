using OpenIddict.Abstractions;
using TomorrowDAOServer.Auth.Common;
using TomorrowDAOServer.Auth.Dtos;
using TomorrowDAOServer.Auth.Verifier.Providers;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;

namespace TomorrowDAOServer.Auth.Verifier;

public interface IVerifierService
{
    Task<VerifierResultDto> VerifyUserInfoAsync(string loginType, ExtensionGrantContext context);
}

[DisableAuditing]
public class VerifierService : IVerifierService, ISingletonDependency
{
    private readonly ILogger<VerifierService> _logger;
    private readonly Dictionary<string, IVerifierProvider> _verifierProviders;

    public VerifierService(ILogger<VerifierService> logger, IEnumerable<IVerifierProvider> verifierProviders)
    {
        _logger = logger;
        _verifierProviders = verifierProviders.ToDictionary(t => t.GetLoginType(), t => t);
    }


    public async Task<VerifierResultDto> VerifyUserInfoAsync(string loginType, ExtensionGrantContext context)
    {
        var verifierProvider = _verifierProviders.GetValueOrDefault(loginType, null);
        if (verifierProvider == null)
        {
            return new VerifierResultDto
            {
                IsVerified = false,
                ForbidResult = ForbidResultHelper.GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    "login_type cannot be null.")
            };
        }

        return await verifierProvider.VerifyUserInfoAsync(context);
    }
}