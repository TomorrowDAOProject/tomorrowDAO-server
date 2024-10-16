using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CoinGecko.Entities.Response.Simple;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Token;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.Caching;

namespace TomorrowDAOServer.ThirdPart.Exchange;

public class CoinGeckoProvider : AbstractExchangeProvider
{
    private const string FiatCurrency = "usd";
    private const string SimplePriceUri = "/simple/price";


    private readonly IOptionsMonitor<ExchangeOptions> _exchangeOptions;
    private readonly ILogger<CoinGeckoProvider> _logger;
    private readonly IOptionsMonitor<CoinGeckoOptions> _coinGeckoOptions;
    private readonly IHttpProvider _httpProvider;

    public CoinGeckoProvider(IOptionsMonitor<CoinGeckoOptions> coinGeckoOptions, IHttpProvider httpProvider,
        ILogger<CoinGeckoProvider> logger, IDistributedCache<TokenExchangeDto> exchangeCache,
        IOptionsMonitor<ExchangeOptions> exchangeOptions) : base(exchangeCache, exchangeOptions)
    {
        _coinGeckoOptions = coinGeckoOptions;
        _httpProvider = httpProvider;
        _logger = logger;
        _exchangeOptions = exchangeOptions;
    }


    public override ExchangeProviderName Name()
    {
        return ExchangeProviderName.CoinGecko;
    }

    public override async Task<TokenExchangeDto> LatestAsync(string fromSymbol, string toSymbol)
    {
        var from = MappingSymbol(fromSymbol);
        var to = MappingSymbol(toSymbol);
        var url = _coinGeckoOptions.CurrentValue.BaseUrl + SimplePriceUri;
        Log.Debug("CoinGecko url {Url}", url);

        var price = await _httpProvider.InvokeAsync<Price>(HttpMethod.Get,
            _coinGeckoOptions.CurrentValue.BaseUrl + SimplePriceUri,
            header: new Dictionary<string, string>
            {
                ["x-cg-pro-api-key"] = _coinGeckoOptions.CurrentValue.ApiKey
            },
            param: new Dictionary<string, string>
            {
                ["ids"] = string.Join(CommonConstant.Comma, from, to),
                ["vs_currencies"] = FiatCurrency
            });
        AssertHelper.IsTrue(price.ContainsKey(from), "CoinGecko not support symbol {}", from);
        AssertHelper.IsTrue(price.ContainsKey(to), "CoinGecko not support symbol {}", to);
        var exchange = price[from][FiatCurrency] / price[to][FiatCurrency];
        return new TokenExchangeDto
        {
            FromSymbol = fromSymbol,
            ToSymbol = toSymbol,
            Exchange = (decimal)exchange,
            Timestamp = DateTime.UtcNow.ToUtcMilliSeconds()
        };
    }

    private string MappingSymbol(string sourceSymbol)
    {
        return _coinGeckoOptions?.CurrentValue?.CoinIdMapping?.TryGetValue(sourceSymbol, out var result) ?? false
            ? result
            : sourceSymbol;
    }

    public override Task<TokenExchangeDto> HistoryAsync(string fromSymbol, string toSymbol, long timestamp)
    {
        throw new NotSupportedException();
    }
}