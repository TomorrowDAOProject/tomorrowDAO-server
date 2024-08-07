using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common.Cache;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;
using HttpMethod = System.Net.Http.HttpMethod;

namespace TomorrowDAOServer.Common.Security;

public interface ISecretProvider
{
    Task<string> GetSecretWithCacheAsync(string key);
}

public class SecretProvider : ISecretProvider, ITransientDependency
{
    private const string GetSecurityUri = "/api/app/thirdPart/secret";

    private readonly ILogger<SecretProvider> _logger;
    private readonly IOptionsMonitor<SecurityServerOptions> _securityServerOption;
    private readonly ILocalMemoryCache<string> _secretCache;
    private readonly IHttpProvider _httpProvider;

    public SecretProvider(ILogger<SecretProvider> logger, IOptionsMonitor<SecurityServerOptions> securityServerOption,
        ILocalMemoryCache<string> secretCache, IHttpProvider httpProvider)
    {
        _logger = logger;
        _securityServerOption = securityServerOption;
        _secretCache = secretCache;
        _httpProvider = httpProvider;
    }

    private string Uri(string path)
    {
        return _securityServerOption.CurrentValue.BaseUrl.TrimEnd('/') + path;
    }

    public async Task<string> GetSecretWithCacheAsync(string key)
    {
        return await _secretCache.GetOrAddAsync(key, async () => await GetSecretAsync(key), new MemoryCacheEntryOptions
        {
            AbsoluteExpiration =
                DateTimeOffset.Now.AddSeconds(_securityServerOption.CurrentValue.SecretCacheSeconds)
        });
    }

    private async Task<string> GetSecretAsync(string key)
    {
        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<string>>(HttpMethod.Get,
            Uri(GetSecurityUri),
            param: new Dictionary<string, string>
            {
                ["key"] = key
            },
            header: SecurityServerHeader(key));
        AssertHelper.NotEmpty(resp?.Data, "Secret response data empty");
        AssertHelper.IsTrue(resp!.Success, "Secret response failed {}", resp.Message);
        return EncryptionHelper.DecryptFromHex(resp.Data, _securityServerOption.CurrentValue.AppSecret);
    }

    public Dictionary<string, string> SecurityServerHeader(params string[] signValues)
    {
        var signString = string.Join(CommonConstant.EmptyString, signValues);
        return new Dictionary<string, string>
        {
            ["appid"] = _securityServerOption.CurrentValue.AppId,
            ["signature"] = EncryptionHelper.EncryptHex(signString, _securityServerOption.CurrentValue.AppSecret)
        };
    }
}