using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Dtos;
using TomorrowDAOServer.Token.Dto;

namespace TomorrowDAOServer.Token;

public interface ITokenService
{
    Task<TokenGrainDto> GetTokenAsync(string chainId, string symbol);
    
    Task<TokenPriceDto> GetTokenPriceAsync(string baseCoin, string quoteCoin);
    
    Task<TokenDto> GetTokenByExplorerAsync(string chainId, string symbol);
    
    Task<double> GetTvlAsync(string chainId);

    Task<Dictionary<string, TokenExchangeDto>> GetExchangePriceAsync(string baseCoin, string quoteCoin);
}