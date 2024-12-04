using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;

namespace TomorrowDAOServer.Auth.Http;

public class HttpClientService : IHttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpClientService> _logger;

    public HttpClientService(IHttpClientFactory httpClientFactory, ILogger<HttpClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    
    
    public async Task<T> GetAsync<T>(string url, Dictionary<string, string> param, int timeout = 10, IContractResolver resolver = null)
    {
        var client = _httpClientFactory.CreateClient();
        if (timeout > 0)
        {
            client.Timeout = TimeSpan.FromSeconds(timeout);
        }

        var fullUrl = PathParamUrl(url, param);
        
        var response = await client.GetStringAsync(fullUrl);
        return JsonConvert.DeserializeObject<T>(response);
    }

    public async Task<T> PostAsync<T>(string url, object paramObj, int timeout = 10, IContractResolver resolver = null)
    {
        return await PostJsonAsync<T>(url, paramObj, null, timeout, resolver);
    }

    private async Task<T> PostJsonAsync<T>(string url, object paramObj, Dictionary<string, string> headers, int timeout,
        IContractResolver resolver = null)
    {
        var requestInput = paramObj == null ? string.Empty : JsonConvert.SerializeObject(paramObj, Formatting.None);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient();
        if (timeout > 0)
        {
            client.Timeout = TimeSpan.FromSeconds(timeout);
        }

        if (headers is { Count: > 0 })
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();

        if (!ResponseSuccess(response.StatusCode))
        {
            _logger.LogError(
                "Response not success, url:{url}, code:{code}, message: {message}, params:{param}",
                url, response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return resolver == null
            ? JsonConvert.DeserializeObject<T>(content)
            : JsonConvert.DeserializeObject<T>(content, new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = resolver
            });
    }

    private bool ResponseSuccess(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.OK or HttpStatusCode.NoContent;
    
    private static string PathParamUrl(string url, Dictionary<string, string> pathParams)
    {
        return pathParams.IsNullOrEmpty()
            ? url
            : pathParams.Aggregate(url, (current, param) => current.Replace($"{{{param.Key}}}", param.Value));
    }
}