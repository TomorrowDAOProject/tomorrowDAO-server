using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.TonGift.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.TonGift.Provider;

/**
 * https://tongifts.notion.site/TonGIfts-API-5b10982488de4662a31500e8815a7b4e
 * https://tongifts.notion.site/TonGifts-API-Doc-55a6a8aa9ddc47f2b35150e0951561c9
 */
public interface ITonGiftApiProvider
{
    Task<TonGiftsResponseDto> UpdateTaskAsync(List<string> userIds);
}

public class TonGiftApiProvider : ITonGiftApiProvider, ISingletonDependency
{
    private readonly IOptionsMonitor<TonGiftTaskOptions> _tonGiftTaskOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TonGiftApiProvider> _logger;

    public TonGiftApiProvider(IOptionsMonitor<TonGiftTaskOptions> tonGiftTaskOptions, IHttpClientFactory httpClientFactory, 
        ILogger<TonGiftApiProvider> logger)
    {
        _tonGiftTaskOptions = tonGiftTaskOptions;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TonGiftsResponseDto> UpdateTaskAsync(List<string> userIds)
    {
        var taskId = _tonGiftTaskOptions.CurrentValue.TaskId;
        var merchantId = _tonGiftTaskOptions.CurrentValue.MerchantId;
        var apiKey = _tonGiftTaskOptions.CurrentValue.ApiKey;
        var url = _tonGiftTaskOptions.CurrentValue.Url;
        var param = new TonGiftsRequestDto
        {
            Status = "completed",
            UserIds = userIds, TaskId = taskId, K = merchantId,
            T = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
        };
        param.S = HMACSHA256Helper.GenerateSignature(param, apiKey);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "curl/7.68.0");
        var tokenParam = JsonConvert.SerializeObject(param);
        var requestParam = new StringContent(tokenParam, Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await client.PostAsync(url, requestParam);
        var result = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("TonGiftsUpdateTaskAsync taskId = {0} param = {1} response = {2} ", taskId, tokenParam, result);
        return JsonConvert.DeserializeObject<TonGiftsResponseDto>(result);
    }
}

public static class ExplorerApi
{
    public static readonly ApiInfo UpdateTask = new(HttpMethod.Post, "/api/open/updateTask");
}