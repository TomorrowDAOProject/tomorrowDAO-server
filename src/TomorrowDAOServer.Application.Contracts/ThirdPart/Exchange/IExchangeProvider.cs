using System.Threading.Tasks;
using TomorrowDAOServer.Token;

namespace TomorrowDAOServer.ThirdPart.Exchange;

public interface IExchangeProvider
{
    public ExchangeProviderName Name();

    public Task<TokenExchangeDto> LatestWithCacheAsync(string fromSymbol, string toSymbol);
    
    public Task<TokenExchangeDto> LatestAsync(string fromSymbol, string toSymbol);

    public Task<TokenExchangeDto> HistoryAsync(string fromSymbol, string toSymbol, long timestamp);

}


public enum ExchangeProviderName
{
    Binance,
    Okx,
    CoinGecko,
    Awaken
}