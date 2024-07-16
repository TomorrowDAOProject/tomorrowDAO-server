using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.Token.Dto;
using TomorrowDAOServer.Treasury.Dto;

namespace TomorrowDAOServer.Treasury;

public interface ITreasuryAssetsService
{
    Task<TreasuryAssetsPagedResultDto> GetTreasuryAssetsAsync(GetTreasuryAssetsInput input);

    Task<double> GetTreasuryAssetsAmountAsync(GetTreasuryAssetsInput input,
        Tuple<Dictionary<string, TokenGrainDto>, Dictionary<string, TokenPriceDto>> tokenInfo);

    Task<Tuple<Dictionary<string, TokenGrainDto>, Dictionary<string, TokenPriceDto>>> GetTokenInfoAsync(
        string chainId, ISet<string> symbols);
}