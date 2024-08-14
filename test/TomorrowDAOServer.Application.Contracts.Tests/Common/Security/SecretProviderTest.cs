using AElf.Types;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common.Cache;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Common.Security;
using TomorrowDAOServer.Options;
using Xunit;

namespace TomorrowDAOServer.Application.Contracts.Tests.Common.Security;

public class SecretProviderTest
{
    private readonly ILogger<SecretProvider> _logger;
    private readonly IOptionsMonitor<SecurityServerOptions> _securityServerOption;
    private readonly ILocalMemoryCache<string> _secretCache;
    private readonly IHttpProvider _httpProvider;
    private readonly ISecretProvider _provider;

    public SecretProviderTest()
    {
        _logger = Substitute.For<ILogger<SecretProvider>>();
        _securityServerOption = Substitute.For<IOptionsMonitor<SecurityServerOptions>>();
        _secretCache = Substitute.For<ILocalMemoryCache<string>>();
        _httpProvider = Substitute.For<IHttpProvider>();
        _provider = new SecretProvider(_logger, _securityServerOption, _secretCache, _httpProvider);
    }
    
    [Fact]
    public async void GetSecretWithCacheAsync_Test()
    {
        _securityServerOption.CurrentValue.Returns(new SecurityServerOptions());
        _secretCache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<string>>>(), Arg.Any<MemoryCacheEntryOptions>())
            .Returns("secret");
        var result = await _provider.GetSecretWithCacheAsync("key");
        result.ShouldBe("secret");
    }
    
    [Fact]
    public async void GetSignatureFromHashAsync_Test()
    {
        _securityServerOption.CurrentValue.Returns(new SecurityServerOptions { BaseUrl = "url", AppId = "appid", AppSecret = "secret"});
        _httpProvider.InvokeAsync<CommonResponseDto<SecretProvider.SignResponseDto>>(
                Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
                Arg.Any<Dictionary<string, string>>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
                Arg.Any<JsonSerializerSettings>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(new CommonResponseDto<SecretProvider.SignResponseDto>
            {
                Data = new SecretProvider.SignResponseDto { Signature = "sig" }, Message = "", Code = "20000"
            });
        var result = await _provider.GetSignatureFromHashAsync("key", Hash.Empty);
        result.ShouldNotBeNull();
    }
}