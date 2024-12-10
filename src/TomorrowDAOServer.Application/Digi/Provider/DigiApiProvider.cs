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

    // [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default, MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName,
    //     TargetType = typeof(TmrwDaoExceptionHandler), Message = "DigiCheckAsyncError", LogTargets = new []{"uid"})]
    public virtual async Task<bool> CheckAsync(long uid)
    {
        try
        {
            var domain = _digiOptions.CurrentValue.Domain;
            var authorizationToken = _digiOptions.CurrentValue.Authorization;
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationToken);
            var requestBody = new { Uid = uid };
            var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, MediaTypeNames.Application.Json);
            _logger.LogInformation("ReportAsyncStart uid {0}, authorizationToken {1} requestContent {2}", uid, authorizationToken, requestContent);
            var response = await _httpClient.PostAsync(domain + DigiApi.Check.Path, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("ReportAsyncEnd uid {0}, authorizationToken {1} requestContent {2} responseContent {3}", uid, authorizationToken, requestContent, responseContent);
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