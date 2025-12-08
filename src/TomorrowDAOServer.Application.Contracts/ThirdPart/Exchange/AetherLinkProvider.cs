using System;
using System.Threading.Tasks;
using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Dtos;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Token;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using Volo.Abp.Caching;

namespace TomorrowDAOServer.ThirdPart.Exchange;

public class AetherLinkProvider : AbstractExchangeProvider
{
    private readonly IOptionsMonitor<ExchangeOptions> _exchangeOptions;
    private readonly IPriceServerProvider _priceServerProvider;

    public AetherLinkProvider(IOptionsMonitor<ExchangeOptions> exchangeOptions, IPriceServerProvider priceServerProvider,
        IDistributedCache<TokenExchangeDto> exchangeCache) : base(exchangeCache, exchangeOptions)
    {
        _exchangeOptions = exchangeOptions;
        _priceServerProvider = priceServerProvider;
    }

    public override ExchangeProviderName Name()
    {
        return ExchangeProviderName.AetherLink;
    }

    public override async Task<TokenExchangeDto> LatestAsync(string fromSymbol, string toSymbol)
    {
        var res = await _priceServerProvider.GetAggregatedTokenPriceAsync(new GetAggregatedTokenPriceRequestDto
        {
            TokenPair = GuidHelper.GenerateGrainId(fromSymbol, toSymbol), AggregateType = AggregateType.Avg
        });
        AssertHelper.NotNull(res.Data, "QueryAetherLinkPriceFailed data is null");
        return new TokenExchangeDto
        {
            FromSymbol = fromSymbol, ToSymbol = toSymbol, Exchange = (decimal)(res.Data.Price / Math.Pow(10, (double)res.Data.Decimal)),
            Timestamp = DateTime.UtcNow.WithMicroSeconds(0).WithMilliSeconds(0).WithSeconds(0).ToUtcMilliSeconds()
        };
    }
}

