using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Token.Dto;

namespace TomorrowDAOServer.Token;

public interface ITokenService
{
    Task<TokenInfoDto> GetTokenInfoAsync(string chainId, string symbol);
    
    Task<TokenPriceDto> GetTokenPriceAsync(string baseCoin, string quoteCoin);
    
    Task<double> GetTvlAsync(string chainId);

    Task<Dictionary<string, TokenExchangeDto>> UpdateExchangePriceAsync(string baseCoin, string quoteCoin);
}