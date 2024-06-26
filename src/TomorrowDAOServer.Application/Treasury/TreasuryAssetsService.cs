using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.Token.Dto;
using TomorrowDAOServer.Treasury.Dto;
using TomorrowDAOServer.Treasury.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Treasury;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TreasuryAssetsService : TomorrowDAOServerAppService, ITreasuryAssetsService
{
    private readonly ILogger<TreasuryAssetsService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly ITreasuryAssetsProvider _treasuryAssetsProvider;
    private readonly ITokenService _tokenService;

    public TreasuryAssetsService(ILogger<TreasuryAssetsService> logger, ITreasuryAssetsProvider treasuryAssetsProvider,
        IObjectMapper objectMapper, ITokenService tokenService)
    {
        _logger = logger;
        _treasuryAssetsProvider = treasuryAssetsProvider;
        _objectMapper = objectMapper;
        _tokenService = tokenService;
    }

    public async Task<TreasuryAssetsPagedResultDto> GetTreasuryAssetsAsync(GetTreasuryAssetsInput input)
    {
        try
        {
            if (input == null || input.DaoId.IsNullOrWhiteSpace() || input.ChainId.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("request parameters daoId or chainId cannot be empty.");
            }

            var resultDto = new TreasuryAssetsPagedResultDto();
            var treasuryAssetsResult = await _treasuryAssetsProvider.GetTreasuryAssetsAsync(input);
            if (treasuryAssetsResult.Item1 <= 0)
            {
                return resultDto;
            }

            var treasuryFundDtos = treasuryAssetsResult.Item2;
            var treasuryFundDto = treasuryFundDtos.FirstOrDefault();
            resultDto.TreasuryAddress = treasuryFundDto?.TreasuryAddress;
            resultDto.DaoId = treasuryFundDto?.DaoId;
            resultDto.Data =
                _objectMapper.Map<List<TreasuryFundDto>, List<TreasuryAssetsDto>>(treasuryAssetsResult.Item2);
            resultDto.TotalCount = treasuryAssetsResult.Item1;

            var symbols = new List<string>();
            foreach (var assetsDto in resultDto.Data.Where(assetsDto => !symbols.Contains(assetsDto.Symbol)))
            {
                symbols.Add(assetsDto.Symbol);
            }

            var (tokenInfoDictionary, tokenPriceDictionary) = await GetTokenInfoAsync(input.ChainId, symbols);

            foreach (var dto in resultDto.Data.Where(dto => tokenInfoDictionary.ContainsKey(dto.Symbol)))
            {
                dto.Decimal = tokenInfoDictionary[dto.Symbol].Decimals;
                dto.UsdValue = dto.Amount / Math.Pow(10, dto.Decimal) * (double)(tokenPriceDictionary.GetValueOrDefault(dto.Symbol)?.Price ?? 0);
            }

            return resultDto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get treasury assets error. daoId={0}, chainId={1}", input?.DaoId, input?.ChainId);
            throw;
        }
    }

    private async Task<Tuple<Dictionary<string, TokenGrainDto>, Dictionary<string, TokenPriceDto>>> GetTokenInfoAsync(string chainId, List<string> symbols)
    {
        var tokenInfoTasks = symbols.Select(symbol => _tokenService.GetTokenAsync(chainId, symbol)).ToList();
        var tokenPriceTasks = symbols.Select(symbol => _tokenService.GetTokenPriceAsync(symbol, CommonConstant.USD)).ToList();
        var tokenInfoResults = (await Task.WhenAll(tokenInfoTasks)).Where(x => x != null)
            .ToDictionary(x => x.Symbol, x => x); 
        var priceResults = (await Task.WhenAll(tokenPriceTasks)).Where(x => x != null)
            .ToDictionary(x => x.BaseCoin, x => x);
        return new Tuple<Dictionary<string, TokenGrainDto>, Dictionary<string, TokenPriceDto>>(tokenInfoResults, priceResults);
    }
}