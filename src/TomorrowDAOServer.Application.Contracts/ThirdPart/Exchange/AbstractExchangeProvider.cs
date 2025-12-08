using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Token;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.ThirdPart.Exchange;

public abstract class AbstractExchangeProvider : IExchangeProvider, ISingletonDependency
{

    private readonly IOptionsMonitor<ExchangeOptions> _exchangeOptions;
    private readonly IDistributedCache<TokenExchangeDto> _exchangeCache;

    protected AbstractExchangeProvider(IDistributedCache<TokenExchangeDto> exchangeCache, IOptionsMonitor<ExchangeOptions> exchangeOptions)
    {
        _exchangeCache = exchangeCache;
        _exchangeOptions = exchangeOptions;
    }


    public abstract ExchangeProviderName Name();
    public abstract Task<TokenExchangeDto> LatestAsync(string fromSymbol, string toSymbol);

    public virtual Task<TokenExchangeDto> HistoryAsync(string fromSymbol, string toSymbol, long timestamp)
    {
        return Task.FromResult(new TokenExchangeDto());
    }

    public async Task<TokenExchangeDto> LatestWithCacheAsync(string fromSymbol, string toSymbol)
    {
        var cacheKey = string.Join(Name().ToString(), fromSymbol, toSymbol);
        return await _exchangeCache.GetOrAddAsync(cacheKey, () => LatestAsync(fromSymbol, toSymbol), () =>
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.UtcNow.AddSeconds(_exchangeOptions.CurrentValue.DataExpireSeconds)
            });
    }
}