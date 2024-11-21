using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.LuckyBox.Provider;

public interface ILuckyboxApiProvider
{
    Task<bool> ReportAsync(string trackId);
}

public class LuckyboxApiProvider : ILuckyboxApiProvider, ISingletonDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<LuckyboxOptions> _luckyboxOptions;
    private readonly ILogger<LuckyboxApiProvider> _logger;

    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    public LuckyboxApiProvider(IHttpProvider httpProvider, IOptionsMonitor<LuckyboxOptions> luckyboxOptions)
    {
        _httpProvider = httpProvider;
        _luckyboxOptions = luckyboxOptions;
    }

    public async Task<bool> ReportAsync(string trackId)
    {
        var domain = _luckyboxOptions.CurrentValue.Domain;
        var apiKey = _luckyboxOptions.CurrentValue.ApiKey;
        var sign = HMACSHA256Helper.HMAC_SHA256(trackId, apiKey);
        try
        {
            var response = await _httpProvider.InvokeAsync<LuckboxResponse>(domain, LuckyboxApi.Report,
                param: MapHelper.ToDictionary(new LuckyboxRequest { TrackId = trackId, Sign = sign }),
                withInfoLog: false, withDebugLog: false, settings: DefaultJsonSettings);
            var success = response.Success;
            _logger.LogInformation("ReportAsyncResponse trackId {0}, sign {1}, success {2}", trackId, sign, success);
            return success;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ReportAsyncException trackId {0}, sign {1}", trackId, sign);
            return false;
        }
    }
}

public static class LuckyboxApi 
{
    public static readonly ApiInfo Report = new(HttpMethod.Post, "/cctip/v1/campaign/task/report");
}