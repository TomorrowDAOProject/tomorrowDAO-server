using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Digi.Provider;

public interface IDigiApiProvider
{
    Task<bool> CheckAsync(long uid);
}

public class DigiApiProvider : IDigiApiProvider, ISingletonDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<DigiOptions> _digiOptions;
    private readonly ILogger<DigiApiProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;

    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    public DigiApiProvider(IHttpProvider httpProvider, IOptionsMonitor<DigiOptions> digiOptions, ILogger<DigiApiProvider> logger, 
        IHttpClientFactory httpClientFactory)
    {
        _httpProvider = httpProvider;
        _digiOptions = digiOptions;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpClient = _httpClientFactory.CreateClient();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler), 
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, ReturnDefault = ReturnDefault.Default, 
        Message = "DigiCheckAsyncError", LogTargets = new []{"uid"})]
    public virtual async Task<bool> CheckAsync(long uid)
    {
        try
        {
            var domain = _digiOptions.CurrentValue.Domain;
            var authorizationToken = _digiOptions.CurrentValue.Authorization;
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationToken);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
            var requestBody = new { Uid = uid };
            var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, MediaTypeNames.Application.Json);
            _logger.LogInformation("ReportAsyncStart uid {0}, authorizationToken {1}", uid, authorizationToken);
            var response = await httpClient.PostAsync(domain + DigiApi.Check.Path, requestContent);
            _logger.LogInformation("ReportAsyncEnd1 uid {0}, authorizationToken {1}, code {2}", uid, authorizationToken, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("ReportAsyncEnd2 uid {0}, authorizationToken {1} responseContent {2}, code {3}", uid, authorizationToken, responseContent, response.StatusCode);
            var digiResponse = JsonConvert.DeserializeObject<DigiResponse>(responseContent) ?? new DigiResponse();
            _logger.LogInformation("ReportAsyncResponse uid {0}, code {1}", uid, digiResponse.Code);
            return digiResponse.Success;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ReportAsyncError");
            return false;
        }
    }
}

public static class DigiApi 
{
    public static readonly ApiInfo Check = new(HttpMethod.Post, "/earn/task/check-ref");
}