using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.ThirdPart.Exchange;
using TomorrowDAOServer.Token.Dto;

namespace TomorrowDAOServer.Token;

public interface ITokenService
{
    Task<TokenInfoDto> GetTokenInfoAsync(string chainId, string symbol);
    Task<TokenInfoDto> GetTokenInfoWithoutUpdateAsync(string chainId, string symbol);
    
    Task<TokenPriceDto> GetTokenPriceAsync(string baseCoin, string quoteCoin);
    
    Task<TvlDetail> GetTvlAsync(string chainId);

    Task UpdateExchangePriceAsync(string baseCoin, string quoteCoin, List<ExchangeProviderName> providerNames);
}