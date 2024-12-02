using System;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Schrodinger.Provider;

public interface ISchrodingerApiProvider
{
    Task<bool> CheckAsync(string id);
}

public class SchrodingerApiProvider : ISchrodingerApiProvider, ISingletonDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<SchrodingerOptions> _schrodingerOptions;

    public SchrodingerApiProvider(IHttpProvider httpProvider, IOptionsMonitor<SchrodingerOptions> schrodingerOptions)
    {
        _httpProvider = httpProvider;
        _schrodingerOptions = schrodingerOptions;
    }

    [ExceptionHandler(typeof(Exception), Message = "SchrodingerApiProviderCheckAsyncError", 
        ReturnDefault = ReturnDefault.Default, LogTargets = new []{"id"})]
    public virtual async Task<bool> CheckAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }
        var domain = _schrodingerOptions.CurrentValue.Domain;
        var url = $"{domain}{SchrodingerApi.Check.Path}/{id}";
        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<bool>>(SchrodingerApi.Check.Method, url);
        return resp.Data;
    }
}

public static class SchrodingerApi 
{
    public static readonly ApiInfo Check = new(HttpMethod.Get, "/api/app/task/check");
}