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
            withInfoLog: false, withDebugLog: false, param: MapHelper.ToDictionary(new AwakenRequest { Symbol = fromSymbol }));
        AssertHelper.IsTrue(res.Success, "QueryAwakenPriceFailed, msg={Msg}", res.Message);
        AssertHelper.NotEmpty(res.Data, "QueryAwakenPriceEmpty");
        return new TokenExchangeDto
        {
            FromSymbol = fromSymbol, ToSymbol = toSymbol, Exchange = res.Data.SafeToDecimal(),
            Timestamp = DateTime.UtcNow.WithMicroSeconds(0).WithMilliSeconds(0).WithSeconds(0).ToUtcMilliSeconds()
        };
    }
    
    private string BaseUrl()
    {
        return _exchangeOptions.CurrentValue.Awaken.BaseUrl;
    }
}

public class AwakenRequest
{
    public string Symbol { get; set; }
}