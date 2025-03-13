using TomorrowDAOServer.Auth.Dtos;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;

namespace TomorrowDAOServer.Auth.Verifier.Providers;

public interface IVerifierProvider
{
    string GetLoginType();
    Task<VerifierResultDto> VerifyUserInfoAsync(ExtensionGrantContext context);
}