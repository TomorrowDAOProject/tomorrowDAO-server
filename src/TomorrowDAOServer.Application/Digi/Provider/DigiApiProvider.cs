using System;
using System.Net.Http;
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
    Task<bool> CheckAsync(string uid);
}

public class DigiApiProvider : IDigiApiProvider, ISingletonDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<DigiOptions> _digiOptions;
    private readonly ILogger<DigiApiProvider> _logger;

    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    public DigiApiProvider(IHttpProvider httpProvider, IOptionsMonitor<DigiOptions> digiOptions, ILogger<DigiApiProvider> logger)
    {
        _httpProvider = httpProvider;
        _digiOptions = digiOptions;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        Message = "CheckAsyncError", LogTargets = new []{"uid"})]
    public async Task<bool> CheckAsync(string uid)
    {
        var domain = _digiOptions.CurrentValue.Domain;
        var authorization = _digiOptions.CurrentValue.Authorization;
        var response = await _httpProvider.InvokeAsync<DigiResponse>(domain, DigiApi.Check,
            param: MapHelper.ToDictionary(new DigiRequest { Uid = uid}),
            header: MapHelper.ToDictionary(new DigiHeader { Authorization = authorization}),
            withInfoLog: false, withDebugLog: false, settings: DefaultJsonSettings);
        var success = response.Success;
        _logger.LogInformation("ReportAsyncResponse trackId {0}, success {1}", uid, success);
        return success;
    }
}

public static class DigiApi 
{
    public static readonly ApiInfo Check = new(HttpMethod.Post, "/earn/task/check-ref");
}