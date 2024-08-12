using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Token;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using Volo.Abp.Caching;

namespace TomorrowDAOServer.ThirdPart.Exchange;

public static class AwakenApi
{
    public static ApiInfo Price = new(HttpMethod.Get, "/api/app/token/price");
}

public class AwakenProvider : AbstractExchangeProvider
{
    private readonly IOptionsMonitor<ExchangeOptions> _exchangeOptions;
    private readonly IHttpProvider _httpProvider;

    public AwakenProvider(IOptionsMonitor<ExchangeOptions> exchangeOptions, IHttpProvider httpProvider,
        IDistributedCache<TokenExchangeDto> exchangeCache) : base(exchangeCache, exchangeOptions)
    {
        _exchangeOptions = exchangeOptions;
        _httpProvider = httpProvider;
    }

    public override ExchangeProviderName Name()
    {
        return ExchangeProviderName.Awaken;
    }

    public override async Task<TokenExchangeDto> LatestAsync(string fromSymbol, string toSymbol)
    {
        AssertHelper.IsTrue(toSymbol == CommonConstant.USD, "Query Awaken price toSymbol not support");
        var res = await _httpProvider.InvokeAsync<CommonResponseDto<string>>(BaseUrl(), AwakenApi.Price, 
            withInfoLog: false, withDebugLog: false, param: ToDictionary(new AwakenRequest { Symbol = fromSymbol }));
        AssertHelper.IsTrue(res.Success, "Query Awaken price failed, msg={Msg}", res.Message);
        AssertHelper.NotEmpty(res.Data, "Query Awaken price empty");
        return new TokenExchangeDto
        {
            FromSymbol = fromSymbol, ToSymbol = toSymbol, Exchange = res.Data.SafeToDecimal(),
            Timestamp = DateTime.UtcNow.WithMicroSeconds(0).WithMilliSeconds(0).WithSeconds(0).ToUtcMilliSeconds()
        };
    }

    public override async Task<TokenExchangeDto> HistoryAsync(string fromSymbol, string toSymbol, long timestamp)
    {
        return new TokenExchangeDto();
    }
    
    private string BaseUrl()
    {
        return _exchangeOptions.CurrentValue.Awaken.BaseUrl;
    }
    
    private static Dictionary<string, string> ToDictionary(object param)
    {
        switch (param)
        {
            case null:
                return null;
            case Dictionary<string, string> dictionary:
                return dictionary;
            default:
            {
                var json = param as string ?? JsonConvert.SerializeObject(param, JsonSettingsBuilder.New()
                    .WithCamelCasePropertyNamesResolver().IgnoreNullValue().Build());
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
        }
    }
}

public class AwakenRequest
{
    public string Symbol { get; set; }
}